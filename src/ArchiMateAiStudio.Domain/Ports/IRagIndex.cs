using ArchiMateAiStudio.Domain.Rag;

namespace ArchiMateAiStudio.Domain.Ports;

public interface IRagIndex
{
    /// <summary>
    /// Replaces all chunks of the given kind for a model, then upserts the provided inputs.
    /// </summary>
    Task ReplaceByKindAsync(
        Guid modelId,
        RagChunkKind kind,
        IReadOnlyList<RagChunkInput> chunks,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts chunks by (modelId, kind, sourceId) without removing other chunks.
    /// </summary>
    Task UpsertAsync(
        Guid modelId,
        IReadOnlyList<RagChunkInput> chunks,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RagHit>> SearchAsync(
        Guid modelId,
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default);
}
