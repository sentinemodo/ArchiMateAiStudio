using ArchiMateAiStudio.Domain.Ports;
using ArchiMateAiStudio.Domain.Proposals;
using Microsoft.EntityFrameworkCore;

namespace ArchiMateAiStudio.Infrastructure.Persistence.Ef;

public sealed class EfChangeProposalRepository : IChangeProposalRepository
{
    private readonly StudioDbContext _db;

    public EfChangeProposalRepository(StudioDbContext db)
    {
        _db = db;
    }

    public async Task<ChangeProposal> CreateAsync(
        ChangeProposal proposal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(proposal);

        var entity = new ProposalEntity
        {
            Id = proposal.Id,
            ModelId = proposal.ModelId,
            PatchJson = PatchJsonSerializer.Serialize(proposal.Patch),
            Source = proposal.Source,
            Status = proposal.Status.ToString(),
            CreatedAt = proposal.CreatedAt,
        };

        _db.Proposals.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDomain(entity);
    }

    public async Task<ChangeProposal?> GetAsync(
        Guid proposalId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.Proposals
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == proposalId, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyList<ChangeProposal>> ListAsync(
        Guid? modelId = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ProposalEntity> query = _db.Proposals.AsNoTracking();
        if (modelId is not null)
        {
            query = query.Where(p => p.ModelId == modelId.Value);
        }

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
        return items.Select(ToDomain).ToList();
    }

    public async Task<ChangeProposal?> UpdateStatusAsync(
        Guid proposalId,
        ChangeProposalStatus status,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.Proposals.FirstOrDefaultAsync(p => p.Id == proposalId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Status = status.ToString();
        await _db.SaveChangesAsync(cancellationToken);
        return ToDomain(entity);
    }

    private static ChangeProposal ToDomain(ProposalEntity entity) => new()
    {
        Id = entity.Id,
        ModelId = entity.ModelId,
        Patch = PatchJsonSerializer.Deserialize(entity.PatchJson),
        Source = entity.Source,
        Status = Enum.Parse<ChangeProposalStatus>(entity.Status),
        CreatedAt = entity.CreatedAt,
    };
}
