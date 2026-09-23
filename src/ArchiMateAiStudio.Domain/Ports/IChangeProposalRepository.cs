using ArchiMateAiStudio.Domain.Proposals;

namespace ArchiMateAiStudio.Domain.Ports;

public interface IChangeProposalRepository
{
    Task<ChangeProposal> CreateAsync(
        ChangeProposal proposal,
        CancellationToken cancellationToken = default);

    Task<ChangeProposal?> GetAsync(Guid proposalId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChangeProposal>> ListAsync(
        Guid? modelId = null,
        CancellationToken cancellationToken = default);

    Task<ChangeProposal?> UpdateStatusAsync(
        Guid proposalId,
        ChangeProposalStatus status,
        CancellationToken cancellationToken = default);
}
