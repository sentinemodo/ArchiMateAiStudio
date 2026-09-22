using ArchiMateAiStudio.Domain.Models;
using ArchiMateAiStudio.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace ArchiMateAiStudio.Infrastructure.Persistence.Ef;

public sealed class EfArchimateModelRepository : IArchimateModelRepository
{
    private readonly StudioDbContext _db;

    public EfArchimateModelRepository(StudioDbContext db)
    {
        _db = db;
    }

    public async Task<ModelRecord> CreateAsync(
        string name,
        string archimateXml,
        CancellationToken cancellationToken = default)
    {
        var entity = new ModelEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            ArchimateXml = archimateXml,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        _db.Models.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ToRecord(entity);
    }

    public async Task<ModelRecord?> GetAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Models
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == modelId, cancellationToken);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task<IReadOnlyList<ModelRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        var items = await _db.Models
            .AsNoTracking()
            .OrderByDescending(m => m.UpdatedAt)
            .ToListAsync(cancellationToken);
        return items.Select(ToRecord).ToList();
    }

    public async Task<ModelRecord?> SaveAsync(
        Guid modelId,
        string name,
        string archimateXml,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.Models.FirstOrDefaultAsync(m => m.Id == modelId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Name = name;
        entity.ArchimateXml = archimateXml;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return ToRecord(entity);
    }

    public Task<bool> ExistsAsync(Guid modelId, CancellationToken cancellationToken = default) =>
        _db.Models.AsNoTracking().AnyAsync(m => m.Id == modelId, cancellationToken);

    private static ModelRecord ToRecord(ModelEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        ArchimateXml = entity.ArchimateXml,
        UpdatedAt = entity.UpdatedAt,
    };
}
