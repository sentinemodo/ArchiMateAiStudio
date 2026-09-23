using ArchiMateAiStudio.Application.Proposals;
using ArchiMateAiStudio.Application.Rag;
using ArchiMateAiStudio.Archimate.Digest;
using ArchiMateAiStudio.Archimate.Parsing;
using ArchiMateAiStudio.Domain.Patches;
using ArchiMateAiStudio.Domain.Ports;
using ArchiMateAiStudio.Domain.Proposals;

namespace ArchiMateAiStudio.Application.Generate;

public sealed class GenerateModelChangesService
{
    private const string SystemPrompt = """
        You are an ArchiMate 3.2 modeling assistant.
        Return ONLY a JSON ModelPatch object with outputSchemaVersion, elements, relationships, and diagrams.
        Do not wrap the JSON in markdown.
        """;

    private readonly IArchimateModelRepository _models;
    private readonly ILlmChatClient _llm;
    private readonly ChangeProposalService _proposals;
    private readonly IRagIndex _rag;

    public GenerateModelChangesService(
        IArchimateModelRepository models,
        ILlmChatClient llm,
        ChangeProposalService proposals,
        IRagIndex rag)
    {
        _models = models;
        _llm = llm;
        _proposals = proposals;
        _rag = rag;
    }

    public async Task<GenerateModelChangesResult> GenerateAsync(
        Guid modelId,
        string instruction,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instruction))
        {
            return GenerateModelChangesResult.BadRequest("Instruction is required.");
        }

        var model = await _models.GetAsync(modelId, cancellationToken);
        if (model is null)
        {
            return GenerateModelChangesResult.NotFound($"Model '{modelId}' was not found.");
        }

        string digest;
        try
        {
            var document = ArchiMateXmlParser.Parse(model.ArchimateXml);
            digest = ModelDigestBuilder.Build(document);
        }
        catch (Exception ex)
        {
            return GenerateModelChangesResult.BadRequest($"Stored XML is invalid: {ex.Message}");
        }

        var trimmedInstruction = instruction.Trim();
        var hits = await _rag.SearchAsync(
            modelId,
            trimmedInstruction,
            ModelRagIndexer.DefaultTopK,
            cancellationToken);
        var retrieved = ModelRagIndexer.FormatRetrievedContext(hits);

        var userMessage = $"""
            {digest}

            {retrieved}

            USER REQUEST:
            {trimmedInstruction}
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
            return GenerateModelChangesResult.BadRequest($"LLM call failed: {ex.Message}");
        }

        ModelPatch patch;
        try
        {
            patch = ModelPatchParser.Parse(completion.Content);
        }
        catch (Exception ex)
        {
            return GenerateModelChangesResult.BadRequest($"Failed to parse ModelPatch: {ex.Message}");
        }

        var proposal = await _proposals.CreateAsync(
            modelId,
            patch,
            source: "generate",
            cancellationToken);

        if (proposal is null)
        {
            return GenerateModelChangesResult.NotFound($"Model '{modelId}' was not found.");
        }

        return GenerateModelChangesResult.Ok(proposal, BuildSummary(patch));
    }

    private static PatchSummary BuildSummary(ModelPatch patch) => new()
    {
        ElementCount = patch.Elements.Count,
        RelationshipCount = patch.Relationships.Count,
        DiagramCount = patch.Diagrams.Count,
        ElementNames = patch.Elements.Select(e => e.Name).ToList(),
    };
}

public sealed class GenerateModelChangesResult
{
    public required GenerateModelChangesOutcome Outcome { get; init; }

    public ChangeProposal? Proposal { get; init; }

    public PatchSummary? Summary { get; init; }

    public string? Message { get; init; }

    public static GenerateModelChangesResult Ok(ChangeProposal proposal, PatchSummary summary) =>
        new()
        {
            Outcome = GenerateModelChangesOutcome.Success,
            Proposal = proposal,
            Summary = summary,
        };

    public static GenerateModelChangesResult NotFound(string message) =>
        new() { Outcome = GenerateModelChangesOutcome.NotFound, Message = message };

    public static GenerateModelChangesResult BadRequest(string message) =>
        new() { Outcome = GenerateModelChangesOutcome.BadRequest, Message = message };
}

public enum GenerateModelChangesOutcome
{
    Success,
    NotFound,
    BadRequest,
}

public sealed class PatchSummary
{
    public int ElementCount { get; init; }

    public int RelationshipCount { get; init; }

    public int DiagramCount { get; init; }

    public IReadOnlyList<string> ElementNames { get; init; } = [];
}
