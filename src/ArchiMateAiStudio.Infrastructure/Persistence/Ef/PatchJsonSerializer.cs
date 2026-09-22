using System.Text.Json;
using ArchiMateAiStudio.Archimate.Parsing;
using ArchiMateAiStudio.Domain.Patches;
using ArchiMateAiStudio.Domain.Proposals;

namespace ArchiMateAiStudio.Infrastructure.Persistence.Ef;

internal static class PatchJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    public static string Serialize(ModelPatch patch) =>
        JsonSerializer.Serialize(patch, Options);

    public static ModelPatch Deserialize(string json) =>
        ModelPatchParser.Parse(json);
}
