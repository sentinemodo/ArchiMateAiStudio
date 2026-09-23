using System.Text.Json;
using ArchiMateAiStudio.Domain.Rag;

namespace ArchiMateAiStudio.Infrastructure.Persistence.Ef;

public sealed class RagChunkEntity
{
    public Guid Id { get; set; }

    public Guid ModelId { get; set; }

    public string Kind { get; set; } = string.Empty;

    public string SourceId { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    /// <summary>JSON-serialized float[] embedding (MVP; cosine in C#).</summary>
    public string EmbeddingJson { get; set; } = "[]";

    public string MetadataJson { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; }

    public float[] GetEmbedding() =>
        JsonSerializer.Deserialize<float[]>(EmbeddingJson) ?? [];

    public void SetEmbedding(float[] embedding) =>
        EmbeddingJson = JsonSerializer.Serialize(embedding);

    public IReadOnlyDictionary<string, string> GetMetadata() =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(MetadataJson)
        ?? new Dictionary<string, string>();

    public void SetMetadata(IReadOnlyDictionary<string, string>? metadata) =>
        MetadataJson = JsonSerializer.Serialize(
            metadata is null ? new Dictionary<string, string>() : metadata);

    public RagChunkKind GetKind() =>
        Enum.TryParse<RagChunkKind>(Kind, ignoreCase: true, out var kind)
            ? kind
            : RagChunkKind.Document;
}
