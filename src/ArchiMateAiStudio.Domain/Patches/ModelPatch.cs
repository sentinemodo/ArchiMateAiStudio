namespace ArchiMateAiStudio.Domain.Patches;

/// <summary>
/// Archi-LLM-compatible JSON patch contract (ADR-0004).
/// </summary>
public sealed class ModelPatch
{
    public string OutputSchemaVersion { get; init; } = "1.0";

    public IReadOnlyList<ElementPatch> Elements { get; init; } = [];

    public IReadOnlyList<RelationshipPatch> Relationships { get; init; } = [];

    public IReadOnlyList<DiagramPatch> Diagrams { get; init; } = [];

    public IReadOnlyList<string> RemoveElementIds { get; init; } = [];

    public IReadOnlyList<string> RemoveElementFromDiagramIds { get; init; } = [];

    public IReadOnlyList<string> RemoveDiagramNames { get; init; } = [];
}

public sealed class ElementPatch
{
    public required string Type { get; init; }

    public required string Name { get; init; }

    public string? Id { get; init; }

    public string? Documentation { get; init; }

    public IReadOnlyDictionary<string, string> Properties { get; init; } =
        new Dictionary<string, string>();
}

public sealed class RelationshipPatch
{
    public required string Type { get; init; }

    public required string SourceRef { get; init; }

    public required string TargetRef { get; init; }

    public string? Id { get; init; }

    public string? Name { get; init; }
}

public sealed class DiagramPatch
{
    public required string Name { get; init; }

    public string? Viewpoint { get; init; }

    public IReadOnlyList<string> ElementRefs { get; init; } = [];
}
