namespace ArchiMateAiStudio.Domain.Rag;

public enum RagChunkKind
{
    Element,
    Document,
}

public sealed record RagChunkInput(
    RagChunkKind Kind,
    string SourceId,
    string Text,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record RagHit(
    Guid ModelId,
    RagChunkKind Kind,
    string SourceId,
    string Text,
    double Score,
    IReadOnlyDictionary<string, string> Metadata);
