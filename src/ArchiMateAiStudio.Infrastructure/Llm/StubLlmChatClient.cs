using ArchiMateAiStudio.Domain.Ports;

namespace ArchiMateAiStudio.Infrastructure.Llm;

/// <summary>
/// CI-safe LLM stub. Replace with RunPodOpenAiChatClient in Phase 1.
/// </summary>
public sealed class StubLlmChatClient : ILlmChatClient
{
    public Task<LlmCompletion> CompleteAsync(LlmRequest request, CancellationToken cancellationToken = default)
    {
        const string payload = """
            {
              "outputSchemaVersion": "1.0",
              "elements": [],
              "relationships": [],
              "diagrams": []
            }
            """;

        return Task.FromResult(new LlmCompletion(payload, "stop"));
    }
}
