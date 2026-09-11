using ArchiMateAiStudio.Domain.Patches;

namespace ArchiMateAiStudio.Domain.Proposals;

public enum ChangeProposalStatus
{
    Pending,
    Approved,
    Rejected,
}

public sealed class ChangeProposal
{
    public required Guid Id { get; init; }

    public required Guid ModelId { get; init; }

    public required ModelPatch Patch { get; init; }

    public required string Source { get; init; }

    public ChangeProposalStatus Status { get; set; } = ChangeProposalStatus.Pending;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
