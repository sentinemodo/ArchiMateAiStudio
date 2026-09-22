using ArchiMateAiStudio.Application.Generate;
using ArchiMateAiStudio.Application.Proposals;
using ArchiMateAiStudio.Application.Rag;
using ArchiMateAiStudio.Archimate.Digest;
using ArchiMateAiStudio.Archimate.Parsing;
using ArchiMateAiStudio.Domain.Patches;
using ArchiMateAiStudio.Domain.Ports;
using ArchiMateAiStudio.Domain.Proposals;

namespace ArchiMateAiStudio.Application.Ingest;

public sealed class PdfIngestService
{
    public const int MaxExtractedTextChars = 12_000;

    private const string SystemPrompt = """
        You are an ArchiMate 3.2 modeling assistant.
        Map the extracted PDF text into a JSON ModelPatch with outputSchemaVersion, elements, relationships, and diagrams.
        Prefer ApplicationComponent, ApplicationService, BusinessProcess, and Capability where appropriate.
        Return ONLY JSON. Do not wrap the JSON in markdown.
        """;

    private readonly IArchimateModelRepository _models;
    private readonly IPdfTextExtractor _pdf;
    private readonly ILlmChatClient _llm;
    private readonly ChangeProposalService _proposals;
    private readonly IRagIndex _rag;
    private readonly ModelRagIndexer _indexer;

    public PdfIngestService(
        IArchimateModelRepository models,
        IPdfTextExtractor pdf,
        ILlmChatClient llm,
        ChangeProposalService proposals,
        IRagIndex rag,
        ModelRagIndexer indexer)
    {
        _models = models;
        _pdf = pdf;
        _llm = llm;
        _proposals = proposals;
        _rag = rag;
        _indexer = indexer;
    }

    public async Task<PdfIngestResult> IngestAsync(
        Guid modelId,
        byte[] pdfBytes,
        string? fileName = null,
        CancellationToken cancellationToken = default)
    {
        if (pdfBytes is null || pdfBytes.Length == 0)
        {
            return PdfIngestResult.BadRequest("PDF file is required.");
        }

        var model = await _models.GetAsync(modelId, cancellationToken);
        if (model is null)
        {
            return PdfIngestResult.NotFound($"Model '{modelId}' was not found.");
        }

        string extracted;
        try
        {
            extracted = _pdf.ExtractText(pdfBytes);
        }
        catch (Exception ex)
        {
            return PdfIngestResult.BadRequest($"Failed to extract PDF text: {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(extracted))
        {
            return PdfIngestResult.BadRequest("PDF has no extractable text layer.");
        }

        if (extracted.Length > MaxExtractedTextChars)
        {
            extracted = extracted[..MaxExtractedTextChars] + "\n…[truncated]";
        }

        string digest;
        try
        {
            var document = ArchiMateXmlParser.Parse(model.ArchimateXml);
            digest = ModelDigestBuilder.Build(document);
        }
        catch (Exception ex)
        {
            return PdfIngestResult.BadRequest($"Stored XML is invalid: {ex.Message}");
        }

        var displayName = string.IsNullOrWhiteSpace(fileName) ? "document.pdf" : fileName.Trim();

        // Index document chunks before the LLM call so retrieval can ground the map.
        await _indexer.IndexDocumentTextAsync(modelId, extracted, displayName, cancellationToken);

        var hits = await _rag.SearchAsync(
            modelId,
            extracted.Length > 400 ? extracted[..400] : extracted,
            ModelRagIndexer.DefaultTopK,
            cancellationToken);
        var retrieved = ModelRagIndexer.FormatRetrievedContext(hits);

        var userMessage = $"""
            {digest}

            {retrieved}

            SOURCE ARTIFACT: {displayName}

            EXTRACTED TEXT:
            {extracted}

            USER REQUEST:
            Map the extracted content to ArchiMate elements as a ModelPatch.
            """;

        LlmCompletion completion;
        try
        {
            completion = await _llm.CompleteAsync(
                new LlmRequest(
                    SystemPrompt,
                    [new LlmMessage("user", userMessage)]),
                cancellationToken);
        }
        catch (Exception ex)
        {
            return PdfIngestResult.BadRequest($"LLM call failed: {ex.Message}");
        }

        ModelPatch patch;
        try
        {
            patch = ModelPatchParser.Parse(completion.Content);
        }
        catch (Exception ex)
        {
            return PdfIngestResult.BadRequest($"Failed to parse ModelPatch: {ex.Message}");
        }

        var proposal = await _proposals.CreateAsync(
            modelId,
            patch,
            source: "pdf-ingest",
            cancellationToken);

        if (proposal is null)
        {
            return PdfIngestResult.NotFound($"Model '{modelId}' was not found.");
        }

        return PdfIngestResult.Ok(proposal, new PatchSummary
        {
            ElementCount = patch.Elements.Count,
            RelationshipCount = patch.Relationships.Count,
            DiagramCount = patch.Diagrams.Count,
            ElementNames = patch.Elements.Select(e => e.Name).ToList(),
        });
    }
}

public sealed class PdfIngestResult
{
    public required GenerateModelChangesOutcome Outcome { get; init; }

    public ChangeProposal? Proposal { get; init; }

    public PatchSummary? Summary { get; init; }

    public string? Message { get; init; }

    public static PdfIngestResult Ok(ChangeProposal proposal, PatchSummary summary) =>
        new()
        {
            Outcome = GenerateModelChangesOutcome.Success,
            Proposal = proposal,
            Summary = summary,
        };

    public static PdfIngestResult NotFound(string message) =>
        new() { Outcome = GenerateModelChangesOutcome.NotFound, Message = message };

    public static PdfIngestResult BadRequest(string message) =>
        new() { Outcome = GenerateModelChangesOutcome.BadRequest, Message = message };
}
