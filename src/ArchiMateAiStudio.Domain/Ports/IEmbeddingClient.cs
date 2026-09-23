namespace ArchiMateAiStudio.Domain.Ports;

public interface IEmbeddingClient
{
    int Dimensions { get; }

    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);
}
