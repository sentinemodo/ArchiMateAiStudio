using ArchiMateAiStudio.Domain.Ports;
using ArchiMateAiStudio.Domain.Rag;
using ArchiMateAiStudio.Infrastructure.Persistence.Ef;
using Microsoft.EntityFrameworkCore;

namespace ArchiMateAiStudio.Infrastructure.Rag;

/// <summary>
/// PostgreSQL-backed RAG index. Stores embeddings as JSON float[] and ranks with
/// cosine similarity in C# for MVP portability (works without pgvector on Neon).
/// Migration still attempts <c>CREATE EXTENSION IF NOT EXISTS vector</c> for future use.
/// </summary>
public sealed class EfRagIndex : IRagIndex
{
    private readonly StudioDbContext _db;
    private readonly IEmbeddingClient _embeddings;

    public EfRagIndex(StudioDbContext db, IEmbeddingClient embeddings)
    {
        _db = db;
        _embeddings = embeddings;
    }

    public async Task ReplaceByKindAsync(
        Guid modelId,
        RagChunkKind kind,
        IReadOnlyList<RagChunkInput> chunks,
        CancellationToken cancellationToken = default)
    {
        var kindName = kind.ToString();
        var existing = await _db.RagChunks
            .Where(c => c.ModelId == modelId && c.Kind == kindName)
            .ToListAsync(cancellationToken);
        _db.RagChunks.RemoveRange(existing);
        await UpsertCoreAsync(modelId, chunks, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpsertAsync(
        Guid modelId,
        IReadOnlyList<RagChunkInput> chunks,
        CancellationToken cancellationToken = default)
    {
        await UpsertCoreAsync(modelId, chunks, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
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

        var rows = await _db.RagChunks
            .AsNoTracking()
            .Where(c => c.ModelId == modelId)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return [];
        }

        var queryEmbedding = await _embeddings.EmbedAsync(query.Trim(), cancellationToken);
        return rows
            .Select(row => new RagHit(
                row.ModelId,
                row.GetKind(),
                row.SourceId,
                row.Text,
                InMemoryRagIndex.CosineSimilarity(queryEmbedding, row.GetEmbedding()),
                row.GetMetadata()))
            .OrderByDescending(h => h.Score)
            .Take(topK)
            .ToList();
    }

    private async Task UpsertCoreAsync(
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

            var kindName = input.Kind.ToString();
            var existing = await _db.RagChunks
                .FirstOrDefaultAsync(
                    c => c.ModelId == modelId
                         && c.Kind == kindName
                         && c.SourceId == input.SourceId,
                    cancellationToken);

            var embedding = await _embeddings.EmbedAsync(input.Text, cancellationToken);
            if (existing is null)
            {
                var entity = new RagChunkEntity
                {
                    Id = Guid.NewGuid(),
                    ModelId = modelId,
                    Kind = kindName,
                    SourceId = input.SourceId,
                    Text = input.Text,
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                entity.SetEmbedding(embedding);
                entity.SetMetadata(input.Metadata);
                _db.RagChunks.Add(entity);
            }
            else
            {
                existing.Text = input.Text;
                existing.SetEmbedding(embedding);
                existing.SetMetadata(input.Metadata);
            }
        }
    }
}
