namespace ArchiMateAiStudio.Domain.Ports;

public sealed record LlmMessage(string Role, string Content);

public sealed record LlmRequest(
    string SystemPrompt,
    IReadOnlyList<LlmMessage> Messages,
    int? MaxTokens = null,
    float Temperature = 0.2f,
    string? ResponseFormatJsonSchema = null);

public sealed record LlmCompletion(string Content, string? FinishReason = null);

public interface ILlmChatClient
{
    Task<LlmCompletion> CompleteAsync(LlmRequest request, CancellationToken cancellationToken = default);
}
