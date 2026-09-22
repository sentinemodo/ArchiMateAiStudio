using ArchiMateAiStudio.Archimate.Parsing;
using ArchiMateAiStudio.Archimate.Patching;
using ArchiMateAiStudio.Archimate.Serialization;
using ArchiMateAiStudio.Application.Rag;
using ArchiMateAiStudio.Domain.Patches;
using ArchiMateAiStudio.Domain.Ports;
using ArchiMateAiStudio.Domain.Proposals;

namespace ArchiMateAiStudio.Application.Proposals;

public sealed class ChangeProposalService
{
    private readonly IChangeProposalRepository _proposals;
    private readonly IArchimateModelRepository _models;
    private readonly ModelRagIndexer _ragIndexer;

    public ChangeProposalService(
        IChangeProposalRepository proposals,
        IArchimateModelRepository models,
        ModelRagIndexer ragIndexer)
    {
        _proposals = proposals;
        _models = models;
        _ragIndexer = ragIndexer;
    }

    public async Task<ChangeProposal?> CreateAsync(
        Guid modelId,
        ModelPatch patch,
        string? source = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patch);

        if (!await _models.ExistsAsync(modelId, cancellationToken))
        {
            return null;
        }

        var proposal = new ChangeProposal
        {
            Id = Guid.NewGuid(),
            ModelId = modelId,
            Patch = patch,
            Source = string.IsNullOrWhiteSpace(source) ? "manual" : source.Trim(),
            Status = ChangeProposalStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        return await _proposals.CreateAsync(proposal, cancellationToken);
    }

    public Task<IReadOnlyList<ChangeProposal>> ListAsync(
        Guid? modelId = null,
        CancellationToken cancellationToken = default) =>
        _proposals.ListAsync(modelId, cancellationToken);

    public async Task<ApproveProposalResult> ApproveAsync(
        Guid proposalId,
        CancellationToken cancellationToken = default)
    {
        var proposal = await _proposals.GetAsync(proposalId, cancellationToken);
        if (proposal is null)
        {
            return ApproveProposalResult.NotFound();
        }

        if (proposal.Status != ChangeProposalStatus.Pending)
        {
            return ApproveProposalResult.Conflict($"Proposal is {proposal.Status}, expected Pending.");
        }

        var model = await _models.GetAsync(proposal.ModelId, cancellationToken);
        if (model is null)
        {
            return ApproveProposalResult.NotFound($"Model '{proposal.ModelId}' was not found.");
        }

        try
        {
            var document = ArchiMateXmlParser.Parse(model.ArchimateXml);
            var applyResult = ModelPatchApplicator.Apply(document, proposal.Patch);
            if (!applyResult.Success || applyResult.Document is null)
            {
                return ApproveProposalResult.Invalid(applyResult.Errors);
            }

            var xml = ArchiMateXmlSerializer.Serialize(applyResult.Document);
            var saved = await _models.SaveAsync(model.Id, model.Name, xml, cancellationToken);
            if (saved is null)
            {
                return ApproveProposalResult.NotFound($"Model '{proposal.ModelId}' was not found.");
            }

            await _ragIndexer.ReindexElementsAsync(model.Id, applyResult.Document, cancellationToken);

            var updated = await _proposals.UpdateStatusAsync(
                proposalId,
                ChangeProposalStatus.Approved,
                cancellationToken);

            return ApproveProposalResult.Ok(updated ?? proposal);
        }
        catch (Exception ex)
        {
            return ApproveProposalResult.Invalid([$"Failed to apply patch: {ex.Message}"]);
        }
    }

    public async Task<ChangeProposal?> RejectAsync(
        Guid proposalId,
        CancellationToken cancellationToken = default)
    {
        var proposal = await _proposals.GetAsync(proposalId, cancellationToken);
        if (proposal is null)
        {
            return null;
        }

        if (proposal.Status != ChangeProposalStatus.Pending)
        {
            return proposal;
        }

        return await _proposals.UpdateStatusAsync(
            proposalId,
            ChangeProposalStatus.Rejected,
            cancellationToken);
    }
}

public sealed class ApproveProposalResult
{
    public required ApproveProposalOutcome Outcome { get; init; }

    public ChangeProposal? Proposal { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];

    public string? Message { get; init; }

    public static ApproveProposalResult Ok(ChangeProposal proposal) =>
        new() { Outcome = ApproveProposalOutcome.Success, Proposal = proposal };

    public static ApproveProposalResult NotFound(string? message = null) =>
        new() { Outcome = ApproveProposalOutcome.NotFound, Message = message };

    public static ApproveProposalResult Conflict(string message) =>
        new() { Outcome = ApproveProposalOutcome.Conflict, Message = message };

    public static ApproveProposalResult Invalid(IReadOnlyList<string> errors) =>
        new() { Outcome = ApproveProposalOutcome.InvalidPatch, Errors = errors };
}

public enum ApproveProposalOutcome
{
    Success,
    NotFound,
    Conflict,
    InvalidPatch,
}
