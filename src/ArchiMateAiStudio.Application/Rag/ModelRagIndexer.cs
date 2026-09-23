using ArchiMateAiStudio.Archimate.Models;
using ArchiMateAiStudio.Archimate.Parsing;
using ArchiMateAiStudio.Domain.Ports;
using ArchiMateAiStudio.Domain.Rag;

namespace ArchiMateAiStudio.Application.Rag;

public sealed class ModelRagIndexer
{
    public const int DocumentChunkMaxChars = 500;
    public const int DefaultTopK = 5;

    private readonly IRagIndex _rag;

    public ModelRagIndexer(IRagIndex rag)
    {
        _rag = rag;
    }

    public Task ReindexElementsFromXmlAsync(
        Guid modelId,
        string archimateXml,
        CancellationToken cancellationToken = default)
    {
        var document = ArchiMateXmlParser.Parse(archimateXml);
        return ReindexElementsAsync(modelId, document, cancellationToken);
    }

    public Task ReindexElementsAsync(
        Guid modelId,
        ArchimateDocument document,
        CancellationToken cancellationToken = default)
    {
        var chunks = BuildElementChunks(document);
        return _rag.ReplaceByKindAsync(modelId, RagChunkKind.Element, chunks, cancellationToken);
    }

    public Task IndexDocumentTextAsync(
        Guid modelId,
        string text,
        string? artifactName = null,
        CancellationToken cancellationToken = default)
    {
        var chunks = SplitDocumentChunks(text, artifactName);
        return _rag.UpsertAsync(modelId, chunks, cancellationToken);
    }

    public Task<IReadOnlyList<RagHit>> SearchAsync(
        Guid modelId,
        string query,
        int topK = DefaultTopK,
        CancellationToken cancellationToken = default) =>
        _rag.SearchAsync(modelId, query, topK, cancellationToken);

    public static string FormatRetrievedContext(IReadOnlyList<RagHit> hits)
    {
        if (hits.Count == 0)
        {
            return "Retrieved context:\n(none)";
        }

        var lines = new List<string> { "Retrieved context:" };
        for (var i = 0; i < hits.Count; i++)
        {
            var hit = hits[i];
            lines.Add($"[{i + 1}] ({hit.Kind}/{hit.SourceId}, score={hit.Score:F3})");
            lines.Add(hit.Text.Trim());
            lines.Add("");
        }

        return string.Join('\n', lines).TrimEnd();
    }

    public static IReadOnlyList<RagChunkInput> BuildElementChunks(ArchimateDocument document)
    {
        var chunks = new List<RagChunkInput>();
        void Walk(IEnumerable<ArchimateFolder> folders)
        {
            foreach (var folder in folders)
            {
                foreach (var concept in folder.Elements)
                {
                    if (concept.Kind != ArchimateConceptKind.Element)
                    {
                        continue;
                    }

                    var name = concept.Name ?? "(unnamed)";
                    var docs = string.IsNullOrWhiteSpace(concept.Documentation)
                        ? string.Empty
                        : concept.Documentation.Trim();
                    var text = string.IsNullOrEmpty(docs)
                        ? $"Type: {concept.Type}\nName: {name}"
                        : $"Type: {concept.Type}\nName: {name}\nDocumentation: {docs}";

                    chunks.Add(new RagChunkInput(
                        RagChunkKind.Element,
                        concept.Id,
                        text,
                        new Dictionary<string, string>
                        {
                            ["elementType"] = concept.Type,
                            ["name"] = name,
                        }));
                }

                Walk(folder.Folders);
            }
        }

        Walk(document.Folders);
        return chunks;
    }

    public static IReadOnlyList<RagChunkInput> SplitDocumentChunks(string text, string? artifactName)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var paragraphs = text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var chunks = new List<RagChunkInput>();
        var index = 0;
        foreach (var paragraph in paragraphs)
        {
            foreach (var piece in SplitLong(paragraph, DocumentChunkMaxChars))
            {
                index++;
                var sourceId = $"doc:{artifactName ?? "document"}:{index}";
                var metadata = new Dictionary<string, string>
                {
                    ["artifact"] = artifactName ?? "document",
                    ["chunkIndex"] = index.ToString(),
                };
                chunks.Add(new RagChunkInput(RagChunkKind.Document, sourceId, piece, metadata));
            }
        }

        if (chunks.Count == 0)
        {
            // Single block without blank-line paragraphs
            foreach (var piece in SplitLong(text.Trim(), DocumentChunkMaxChars))
            {
                index++;
                chunks.Add(new RagChunkInput(
                    RagChunkKind.Document,
                    $"doc:{artifactName ?? "document"}:{index}",
                    piece,
                    new Dictionary<string, string>
                    {
                        ["artifact"] = artifactName ?? "document",
                        ["chunkIndex"] = index.ToString(),
                    }));
            }
        }

        return chunks;
    }

    private static IEnumerable<string> SplitLong(string text, int maxChars)
    {
        if (text.Length <= maxChars)
        {
            yield return text;
            yield break;
        }

        var start = 0;
        while (start < text.Length)
        {
            var len = Math.Min(maxChars, text.Length - start);
            if (start + len < text.Length)
            {
                var slice = text.AsSpan(start, len);
                var lastSpace = slice.LastIndexOf(' ');
                if (lastSpace > maxChars / 2)
                {
                    len = lastSpace;
                }
            }

            yield return text.Substring(start, len).Trim();
            start += len;
            while (start < text.Length && char.IsWhiteSpace(text[start]))
            {
                start++;
            }
        }
    }
}
