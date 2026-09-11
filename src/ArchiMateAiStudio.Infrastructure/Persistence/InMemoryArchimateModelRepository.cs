using ArchiMateAiStudio.Domain.Ports;

namespace ArchiMateAiStudio.Infrastructure.Persistence;

public sealed class InMemoryArchimateModelRepository : IArchimateModelRepository
{
    public Task<bool> ExistsAsync(Guid modelId, CancellationToken cancellationToken = default) =>
        Task.FromResult(modelId != Guid.Empty);
}
