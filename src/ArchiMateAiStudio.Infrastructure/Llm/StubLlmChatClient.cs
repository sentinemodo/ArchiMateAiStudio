using ArchiMateAiStudio.Domain.Ports;

namespace ArchiMateAiStudio.Infrastructure.Llm;

/// <summary>
/// CI-safe LLM stub. Returns a Claims Portal ApplicationComponent patch when the
/// instruction mentions Claims or ApplicationComponent; otherwise invents an
/// ApplicationComponent from the first meaningful line of extracted text.
/// </summary>
public sealed class StubLlmChatClient : ILlmChatClient
{
    private const string ClaimsPortalPatch = """
        {
          "outputSchemaVersion": "1.0",
          "elements": [
            {
              "type": "ApplicationComponent",
              "name": "Claims Portal",
              "id": "id-bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"
            }
          ],
          "relationships": [],
          "diagrams": []
        }
        """;

    public Task<LlmCompletion> CompleteAsync(LlmRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var haystack = string.Join('\n', request.Messages.Select(m => m.Content));

        if (MentionsClaimsOrApplicationComponent(haystack))
        {
            return Task.FromResult(new LlmCompletion(ClaimsPortalPatch, "stop"));
        }

        var name = ExtractElementName(haystack);
        var patch = BuildApplicationComponentPatch(name);
        return Task.FromResult(new LlmCompletion(patch, "stop"));
    }

    private static bool MentionsClaimsOrApplicationComponent(string text) =>
        text.Contains("Claims", StringComparison.OrdinalIgnoreCase)
        || text.Contains("ApplicationComponent", StringComparison.OrdinalIgnoreCase);

    private static string ExtractElementName(string haystack)
    {
        const string marker = "EXTRACTED TEXT:";
        var idx = haystack.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return "Imported Capability";
        }

        var region = haystack[(idx + marker.Length)..];

        foreach (var rawLine in region.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith("USER REQUEST:", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("SOURCE ARTIFACT:", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("…", StringComparison.Ordinal))
            {
                break;
            }

            if (line.StartsWith("MODEL", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            return TruncateName(line);
        }

        return "Imported Capability";
    }

    private static string TruncateName(string name)
    {
        const int max = 120;
        return name.Length <= max ? name : name[..max].TrimEnd();
    }

    private static string BuildApplicationComponentPatch(string name)
    {
        var escaped = name
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);

        return $$"""
            {
              "outputSchemaVersion": "1.0",
              "elements": [
                {
                  "type": "ApplicationComponent",
                  "name": "{{escaped}}",
                  "id": "id-cccccccccccccccccccccccccccccccc"
                }
              ],
              "relationships": [],
              "diagrams": []
            }
            """;
    }
}
