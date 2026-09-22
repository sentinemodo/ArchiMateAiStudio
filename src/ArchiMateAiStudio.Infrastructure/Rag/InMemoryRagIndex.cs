using System.Collections.Concurrent;
using ArchiMateAiStudio.Domain.Ports;
using ArchiMateAiStudio.Domain.Rag;

namespace ArchiMateAiStudio.Infrastructure.Rag;

public sealed class InMemoryRagIndex : IRagIndex
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, StoredChunk>> _byModel = new();
    private readonly IEmbeddingClient _embeddings;

    public InMemoryRagIndex(IEmbeddingClient embeddings)
    {
        _embeddings = embeddings;
    }

    public async Task ReplaceByKindAsync(
        Guid modelId,
        RagChunkKind kind,
        IReadOnlyList<RagChunkInput> chunks,
        CancellationToken cancellationToken = default)
    {
        var store = _byModel.GetOrAdd(modelId, _ => new ConcurrentDictionary<string, StoredChunk>());
        foreach (var key in store.Keys.Where(k => k.StartsWith(KindPrefix(kind), StringComparison.Ordinal)).ToList())
        {
            store.TryRemove(key, out _);
        }

        await UpsertIntoAsync(store, modelId, chunks, cancellationToken);
    }

    public async Task UpsertAsync(
        Guid modelId,
        IReadOnlyList<RagChunkInput> chunks,
        CancellationToken cancellationToken = default)
    {
        var store = _byModel.GetOrAdd(modelId, _ => new ConcurrentDictionary<string, StoredChunk>());
        await UpsertIntoAsync(store, modelId, chunks, cancellationToken);
    }

    public async Task<IReadOnlyList<RagHit>> SearchAsync(
        Guid modelId,
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        if (topK <= 0 || string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        if (!_byModel.TryGetValue(modelId, out var store) || store.IsEmpty)
        {
            return [];
        }

        var queryEmbedding = await _embeddings.EmbedAsync(query.Trim(), cancellationToken);
        return store.Values
            .Select(chunk => new RagHit(
                chunk.ModelId,
                chunk.Kind,
                chunk.SourceId,
                chunk.Text,
                CosineSimilarity(queryEmbedding, chunk.Embedding),
                chunk.Metadata))
            .OrderByDescending(h => h.Score)
            .Take(topK)
            .ToList();
    }

    private async Task UpsertIntoAsync(
        ConcurrentDictionary<string, StoredChunk> store,
        Guid modelId,
        IReadOnlyList<RagChunkInput> chunks,
        CancellationToken cancellationToken)
    {
        foreach (var input in chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(input.Text))
            {
                continue;
            }

            var embedding = await _embeddings.EmbedAsync(input.Text, cancellationToken);
            var key = ChunkKey(input.Kind, input.SourceId);
            store[key] = new StoredChunk(
                modelId,
                input.Kind,
                input.SourceId,
                input.Text,
                embedding,
                input.Metadata is null
                    ? new Dictionary<string, string>()
                    : new Dictionary<string, string>(input.Metadata));
        }
    }

    private static string KindPrefix(RagChunkKind kind) => $"{kind}:";

    private static string ChunkKey(RagChunkKind kind, string sourceId) =>
        $"{kind}:{sourceId}";

    internal static double CosineSimilarity(float[] a, float[] b)
    {
        var len = Math.Min(a.Length, b.Length);
        double dot = 0;
        double normA = 0;
        double normB = 0;
        for (var i = 0; i < len; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA <= 0 || normB <= 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    private sealed record StoredChunk(
        Guid ModelId,
        RagChunkKind Kind,
        string SourceId,
        string Text,
        float[] Embedding,
        IReadOnlyDictionary<string, string> Metadata);
}
