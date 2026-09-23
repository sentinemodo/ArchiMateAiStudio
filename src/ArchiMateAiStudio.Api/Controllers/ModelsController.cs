using ArchiMateAiStudio.Application.Generate;
using ArchiMateAiStudio.Application.Ingest;
using ArchiMateAiStudio.Application.Rag;
using ArchiMateAiStudio.Archimate.Digest;
using ArchiMateAiStudio.Archimate.Models;
using ArchiMateAiStudio.Archimate.Parsing;
using ArchiMateAiStudio.Domain.Ports;
using Microsoft.AspNetCore.Mvc;

namespace ArchiMateAiStudio.Api.Controllers;

[ApiController]
[Route("api/v1/models")]
public sealed class ModelsController : ControllerBase
{
    private readonly IArchimateModelRepository _repository;
    private readonly GenerateModelChangesService _generate;
    private readonly PdfIngestService _ingest;
    private readonly ModelRagIndexer _ragIndexer;

    public ModelsController(
        IArchimateModelRepository repository,
        GenerateModelChangesService generate,
        PdfIngestService ingest,
        ModelRagIndexer ragIndexer)
    {
        _repository = repository;
        _generate = generate;
        _ingest = ingest;
        _ragIndexer = ragIndexer;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var models = await _repository.ListAsync(cancellationToken);
        var items = models.Select(model =>
        {
            var stats = TryStats(model.ArchimateXml);
            return new
            {
                id = model.Id,
                name = model.Name,
                elementCount = stats?.ElementCount ?? 0,
                updatedAt = model.UpdatedAt,
            };
        });

        return Ok(new { items });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        if (Request.HasFormContentType)
        {
            var file = Request.Form.Files.GetFile("file");
            if (file is not null && file.Length > 0)
            {
                await using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);
                var xml = await reader.ReadToEndAsync(cancellationToken);

                ArchimateDocument document;
                try
                {
                    document = ArchiMateXmlParser.Parse(xml);
                }
                catch (Exception ex)
                {
                    return BadRequest(new { error = $"Invalid .archimate file: {ex.Message}" });
                }

                var importedName = string.IsNullOrWhiteSpace(document.Name)
                    ? Path.GetFileNameWithoutExtension(file.FileName)
                    : document.Name;
                var created = await _repository.CreateAsync(importedName, xml, cancellationToken);
                await TryReindexAsync(created.Id, xml, cancellationToken);
                return CreatedAtAction(
                    nameof(Get),
                    new { id = created.Id },
                    new { id = created.Id, name = created.Name });
            }

            if (Request.Form.TryGetValue("name", out var formName) && !string.IsNullOrWhiteSpace(formName))
            {
                return await CreateEmptyAsync(formName.ToString(), cancellationToken);
            }

            return BadRequest(new { error = "Provide multipart file '.archimate' or form field 'name'." });
        }

        CreateModelRequest? body;
        try
        {
            body = await Request.ReadFromJsonAsync<CreateModelRequest>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Invalid JSON body: {ex.Message}" });
        }

        if (body is null || string.IsNullOrWhiteSpace(body.Name))
        {
            return BadRequest(new { error = "Provide JSON { \"name\": \"...\" } or multipart .archimate file." });
        }

        return await CreateEmptyAsync(body.Name, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var model = await _repository.GetAsync(id, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        ArchimateDocument document;
        try
        {
            document = ArchiMateXmlParser.Parse(model.ArchimateXml);
        }
        catch (Exception ex)
        {
            return UnprocessableEntity(new { error = $"Stored XML is invalid: {ex.Message}" });
        }

        var stats = document.Statistics;
        return Ok(new
        {
            id = model.Id,
            name = model.Name,
            digest = ModelDigestBuilder.Build(document),
            schemaVersion = document.Version ?? "3.2",
            stats = new
            {
                elements = stats.ElementCount,
                relationships = stats.RelationshipCount,
                views = stats.DiagramCount,
            },
            updatedAt = model.UpdatedAt,
        });
    }

    [HttpGet("{id:guid}/export")]
    public async Task<IActionResult> Export(Guid id, CancellationToken cancellationToken)
    {
        var model = await _repository.GetAsync(id, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        var fileName = $"{SanitizeFileName(model.Name)}.archimate";
        return File(
            System.Text.Encoding.UTF8.GetBytes(model.ArchimateXml),
            "application/xml",
            fileName);
    }

    [HttpGet("{id:guid}/search")]
    public async Task<IActionResult> Search(
        Guid id,
        [FromQuery] string? q,
        [FromQuery] int topK = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new { error = "Query parameter 'q' is required." });
        }

        if (!await _repository.ExistsAsync(id, cancellationToken))
        {
            return NotFound();
        }

        var hits = await _ragIndexer.SearchAsync(
            id,
            q.Trim(),
            topK <= 0 ? ModelRagIndexer.DefaultTopK : Math.Min(topK, 20),
            cancellationToken);

        return Ok(new
        {
            items = hits.Select(h => new
            {
                modelId = h.ModelId,
                kind = h.Kind.ToString().ToLowerInvariant(),
                sourceId = h.SourceId,
                text = h.Text,
                score = h.Score,
                metadata = h.Metadata,
            }),
        });
    }

    [HttpPost("{id:guid}/generate")]
    public async Task<IActionResult> Generate(
        Guid id,
        [FromBody] GenerateRequest? request,
        CancellationToken cancellationToken)
    {
        var instruction = request?.Instruction ?? string.Empty;
        var result = await _generate.GenerateAsync(id, instruction, cancellationToken);

        return result.Outcome switch
        {
            GenerateModelChangesOutcome.Success => Ok(new
            {
                proposalId = result.Proposal!.Id,
                status = result.Proposal.Status.ToString(),
                source = result.Proposal.Source,
                summary = new
                {
                    elementCount = result.Summary!.ElementCount,
                    relationshipCount = result.Summary.RelationshipCount,
                    diagramCount = result.Summary.DiagramCount,
                    elementNames = result.Summary.ElementNames,
                },
                patch = result.Proposal.Patch,
            }),
            GenerateModelChangesOutcome.NotFound => NotFound(new { error = result.Message }),
            GenerateModelChangesOutcome.BadRequest => BadRequest(new { error = result.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    [HttpPost("{id:guid}/ingest")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Ingest(Guid id, CancellationToken cancellationToken)
    {
        if (!Request.HasFormContentType)
        {
            return BadRequest(new { error = "Expected multipart form with PDF file field 'file'." });
        }

        var file = Request.Form.Files.GetFile("file");
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "PDF file is required (form field 'file')." });
        }

        await using var stream = file.OpenReadStream();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        var bytes = ms.ToArray();

        var result = await _ingest.IngestAsync(id, bytes, file.FileName, cancellationToken);

        return result.Outcome switch
        {
            GenerateModelChangesOutcome.Success => Ok(new
            {
                proposalId = result.Proposal!.Id,
                status = result.Proposal.Status.ToString(),
                source = result.Proposal.Source,
                summary = new
                {
                    elementCount = result.Summary!.ElementCount,
                    relationshipCount = result.Summary.RelationshipCount,
                    diagramCount = result.Summary.DiagramCount,
                    elementNames = result.Summary.ElementNames,
                },
                patch = result.Proposal.Patch,
            }),
            GenerateModelChangesOutcome.NotFound => NotFound(new { error = result.Message }),
            GenerateModelChangesOutcome.BadRequest => BadRequest(new { error = result.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    private async Task<IActionResult> CreateEmptyAsync(string name, CancellationToken cancellationToken)
    {
        var emptyXml = EmptyArchimateModelFactory.CreateXml(name);
        var empty = await _repository.CreateAsync(name.Trim(), emptyXml, cancellationToken);
        await TryReindexAsync(empty.Id, emptyXml, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = empty.Id }, new { id = empty.Id, name = empty.Name });
    }

    private async Task TryReindexAsync(Guid modelId, string xml, CancellationToken cancellationToken)
    {
        try
        {
            await _ragIndexer.ReindexElementsFromXmlAsync(modelId, xml, cancellationToken);
        }
        catch
        {
            // Reindex is best-effort for MVP; model CRUD should still succeed.
        }
    }

    private static DocumentStatistics? TryStats(string xml)
    {
        try
        {
            return ArchiMateXmlParser.Parse(xml).Statistics;
        }
        catch
        {
            return null;
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "model" : cleaned;
    }

    public sealed class CreateModelRequest
    {
        public string? Name { get; init; }
    }

    public sealed class GenerateRequest
    {
        public string? Instruction { get; init; }
    }
}
