namespace ArchiMateAiStudio.Domain.Ports;

public interface IArchimateModelRepository
{
    Task<bool> ExistsAsync(Guid modelId, CancellationToken cancellationToken = default);
}
