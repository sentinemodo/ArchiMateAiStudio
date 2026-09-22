using ArchiMateAiStudio.Domain.Ports;

namespace ArchiMateAiStudio.Infrastructure.Llm;

/// <summary>
/// CI-safe LLM stub. Replace with RunPodOpenAiChatClient in Phase 1.
/// Returns a Claims Portal ApplicationComponent patch when the instruction
/// mentions that name or type; otherwise an empty elements array.
/// </summary>
public sealed class StubLlmChatClient : ILlmChatClient
{
    private const string EmptyPatch = """
        {
          "outputSchemaVersion": "1.0",
          "elements": [],
          "relationships": [],
          "diagrams": []
        }
        """;

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

        var haystack = string.Join(
            '\n',
            new[] { request.SystemPrompt }
                .Concat(request.Messages.Select(m => m.Content)));

        var payload = MentionsClaimsPortal(haystack) ? ClaimsPortalPatch : EmptyPatch;
        return Task.FromResult(new LlmCompletion(payload, "stop"));
    }

    private static bool MentionsClaimsPortal(string text) =>
        text.Contains("Claims Portal", StringComparison.OrdinalIgnoreCase)
        || text.Contains("ApplicationComponent", StringComparison.OrdinalIgnoreCase);
}
