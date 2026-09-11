namespace ArchiMateAiStudio.Archimate.Models;

public enum ArchimateConceptKind
{
    Element,
    Relationship,
    Diagram,
    DiagramDecoration,
}

public sealed class ArchimateConcept
{
    public required string Id { get; init; }

    public required string Type { get; init; }

    public ArchimateConceptKind Kind { get; init; } = ArchimateConceptKind.Element;

    public string? Name { get; init; }

    public string? Source { get; init; }

    public string? Target { get; init; }

    public string? Viewpoint { get; init; }

    public string? Documentation { get; init; }

    public string? ArchimateElementRef { get; init; }

    public string? ArchimateRelationshipRef { get; init; }

    public ArchimateBounds? Bounds { get; init; }

    public IReadOnlyDictionary<string, string> Properties { get; init; } =
        new Dictionary<string, string>();

    public IReadOnlyDictionary<string, string> Attributes { get; init; } =
        new Dictionary<string, string>();

    public IReadOnlyList<ArchimateConcept> Children { get; init; } = [];
}

public sealed class ArchimateBounds
{
    public int X { get; init; }

    public int Y { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }
}
