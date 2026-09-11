namespace ArchiMateAiStudio.Archimate.Models;

/// <summary>
/// In-memory representation of an Archi-compatible .archimate model.
/// </summary>
public sealed class ArchimateDocument
{
    public required string Name { get; init; }

    public required string Id { get; init; }

    public string? Version { get; init; }

    public IReadOnlyList<ArchimateFolder> Folders { get; init; } = [];

    public DocumentStatistics Statistics => DocumentStatistics.From(this);
}

public sealed class DocumentStatistics
{
    public required int ElementCount { get; init; }

    public required int RelationshipCount { get; init; }

    public required int DiagramCount { get; init; }

    public required int FolderCount { get; init; }

    public static DocumentStatistics From(ArchimateDocument document)
    {
        var folders = 0;
        var elements = 0;
        var relationships = 0;
        var diagrams = 0;

        void Walk(IEnumerable<ArchimateFolder> folderNodes)
        {
            foreach (var folder in folderNodes)
            {
                folders++;

                foreach (var concept in folder.Elements)
                {
                    switch (concept.Kind)
                    {
                        case ArchimateConceptKind.Relationship:
                            relationships++;
                            break;
                        case ArchimateConceptKind.Diagram:
                            diagrams++;
                            break;
                        default:
                            elements++;
                            break;
                    }
                }

                Walk(folder.Folders);
            }
        }

        Walk(document.Folders);

        return new DocumentStatistics
        {
            ElementCount = elements,
            RelationshipCount = relationships,
            DiagramCount = diagrams,
            FolderCount = folders,
        };
    }
}
