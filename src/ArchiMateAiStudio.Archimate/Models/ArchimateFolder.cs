namespace ArchiMateAiStudio.Archimate.Models;

public sealed class ArchimateFolder
{
    public required string Name { get; init; }

    public required string Id { get; init; }

    public string? Type { get; init; }

    public IReadOnlyList<ArchimateFolder> Folders { get; init; } = [];

    public IReadOnlyList<ArchimateConcept> Elements { get; init; } = [];
}
