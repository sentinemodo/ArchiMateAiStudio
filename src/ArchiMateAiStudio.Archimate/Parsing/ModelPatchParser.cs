using System.Text.Json;
using ArchiMateAiStudio.Domain.Patches;

namespace ArchiMateAiStudio.Archimate.Parsing;

public static class ModelPatchParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static ModelPatch Parse(string json)
    {
        var content = ExtractJsonObject(json);
        var patch = JsonSerializer.Deserialize<ModelPatchDto>(content, Options)
            ?? throw new JsonException("Patch payload was empty.");

        return patch.ToDomain();
    }

    internal static string ExtractJsonObject(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            throw new JsonException("No JSON object found in LLM response.");
        }

        return raw[start..(end + 1)];
    }

    private sealed class ModelPatchDto
    {
        public string? OutputSchemaVersion { get; init; }

        public List<ElementPatchDto>? Elements { get; init; }

        public List<RelationshipPatchDto>? Relationships { get; init; }

        public List<DiagramPatchDto>? Diagrams { get; init; }

        public List<string>? RemoveElementIds { get; init; }

        public List<string>? RemoveElementFromDiagramIds { get; init; }

        public List<string>? RemoveDiagramNames { get; init; }

        public ModelPatch ToDomain() => new()
        {
            OutputSchemaVersion = OutputSchemaVersion ?? "1.0",
            Elements = Elements?.Select(static e => e.ToDomain()).ToList() ?? [],
            Relationships = Relationships?.Select(static r => r.ToDomain()).ToList() ?? [],
            Diagrams = Diagrams?.Select(static d => d.ToDomain()).ToList() ?? [],
            RemoveElementIds = RemoveElementIds ?? [],
            RemoveElementFromDiagramIds = RemoveElementFromDiagramIds ?? [],
            RemoveDiagramNames = RemoveDiagramNames ?? [],
        };
    }

    private sealed class ElementPatchDto
    {
        public required string Type { get; init; }

        public required string Name { get; init; }

        public string? Id { get; init; }

        public string? Documentation { get; init; }

        public Dictionary<string, string>? Properties { get; init; }

        public ElementPatch ToDomain() => new()
        {
            Type = Type,
            Name = Name,
            Id = Id,
            Documentation = Documentation,
            Properties = Properties ?? new Dictionary<string, string>(),
        };
    }

    private sealed class RelationshipPatchDto
    {
        public required string Type { get; init; }

        public required string SourceRef { get; init; }

        public required string TargetRef { get; init; }

        public string? Id { get; init; }

        public string? Name { get; init; }

        public RelationshipPatch ToDomain() => new()
        {
            Type = Type,
            SourceRef = SourceRef,
            TargetRef = TargetRef,
            Id = Id,
            Name = Name,
        };
    }

    private sealed class DiagramPatchDto
    {
        public required string Name { get; init; }

        public string? Viewpoint { get; init; }

        public List<string>? ElementRefs { get; init; }

        public DiagramPatch ToDomain() => new()
        {
            Name = Name,
            Viewpoint = Viewpoint,
            ElementRefs = ElementRefs ?? [],
        };
    }
}
