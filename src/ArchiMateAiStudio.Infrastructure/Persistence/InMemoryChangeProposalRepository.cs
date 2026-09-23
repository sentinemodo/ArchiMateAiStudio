using System.Collections.Concurrent;
using ArchiMateAiStudio.Domain.Ports;
using ArchiMateAiStudio.Domain.Proposals;

namespace ArchiMateAiStudio.Infrastructure.Persistence;

/// <summary>
/// Process-lifetime in-memory store of change proposals (Phase 0; EF later).
/// </summary>
public sealed class InMemoryChangeProposalRepository : IChangeProposalRepository
{
    private readonly ConcurrentDictionary<Guid, ChangeProposal> _proposals = new();

    public Task<ChangeProposal> CreateAsync(
        ChangeProposal proposal,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(proposal);

        if (!_proposals.TryAdd(proposal.Id, proposal))
        {
            throw new InvalidOperationException($"Proposal id '{proposal.Id}' already exists.");
        }

        return Task.FromResult(proposal);
    }

    public Task<ChangeProposal?> GetAsync(Guid proposalId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _proposals.TryGetValue(proposalId, out var proposal);
        return Task.FromResult(proposal);
    }

    public Task<IReadOnlyList<ChangeProposal>> ListAsync(
        Guid? modelId = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IEnumerable<ChangeProposal> query = _proposals.Values;
        if (modelId is not null)
        {
            query = query.Where(p => p.ModelId == modelId.Value);
        }

        IReadOnlyList<ChangeProposal> items = query
            .OrderByDescending(p => p.CreatedAt)
            .ToList();
        return Task.FromResult(items);
    }

    public Task<ChangeProposal?> UpdateStatusAsync(
        Guid proposalId,
        ChangeProposalStatus status,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_proposals.TryGetValue(proposalId, out var existing))
        {
            return Task.FromResult<ChangeProposal?>(null);
        }

        existing.Status = status;
        return Task.FromResult<ChangeProposal?>(existing);
    }
}
