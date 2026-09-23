using ArchiMateAiStudio.Domain.Models;

namespace ArchiMateAiStudio.Domain.Ports;

public interface IArchimateModelRepository
{
    Task<ModelRecord> CreateAsync(
        string name,
        string archimateXml,
        CancellationToken cancellationToken = default);

    Task<ModelRecord?> GetAsync(Guid modelId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ModelRecord>> ListAsync(CancellationToken cancellationToken = default);

    Task<ModelRecord?> SaveAsync(
        Guid modelId,
        string name,
        string archimateXml,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid modelId, CancellationToken cancellationToken = default);
}
