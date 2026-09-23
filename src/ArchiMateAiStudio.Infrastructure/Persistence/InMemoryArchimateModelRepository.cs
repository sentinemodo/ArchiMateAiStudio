using System.Collections.Concurrent;
using ArchiMateAiStudio.Domain.Models;
using ArchiMateAiStudio.Domain.Ports;

namespace ArchiMateAiStudio.Infrastructure.Persistence;

/// <summary>
/// Process-lifetime in-memory store of canonical <c>.archimate</c> XML (Phase 0; EF later).
/// </summary>
public sealed class InMemoryArchimateModelRepository : IArchimateModelRepository
{
    private readonly ConcurrentDictionary<Guid, ModelRecord> _models = new();

    public Task<ModelRecord> CreateAsync(
        string name,
        string archimateXml,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var record = new ModelRecord
        {
            Id = Guid.NewGuid(),
            Name = name,
            ArchimateXml = archimateXml,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        if (!_models.TryAdd(record.Id, record))
        {
            throw new InvalidOperationException($"Model id '{record.Id}' already exists.");
        }

        return Task.FromResult(record);
    }

    public Task<ModelRecord?> GetAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _models.TryGetValue(modelId, out var record);
        return Task.FromResult(record);
    }

    public Task<IReadOnlyList<ModelRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<ModelRecord> items = _models.Values
            .OrderByDescending(m => m.UpdatedAt)
            .ToList();
        return Task.FromResult(items);
    }

    public Task<ModelRecord?> SaveAsync(
        Guid modelId,
        string name,
        string archimateXml,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_models.TryGetValue(modelId, out _))
        {
            return Task.FromResult<ModelRecord?>(null);
        }

        var updated = new ModelRecord
        {
            Id = modelId,
            Name = name,
            ArchimateXml = archimateXml,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        _models[modelId] = updated;
        return Task.FromResult<ModelRecord?>(updated);
    }

    public Task<bool> ExistsAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_models.ContainsKey(modelId));
    }
}
