using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ArchiMateAiStudio.Domain.Ports;

namespace ArchiMateAiStudio.Infrastructure.Llm;

/// <summary>
/// OpenAI-compatible chat client for RunPod Serverless (vLLM / TGI handlers).
/// </summary>
public sealed class RunPodOpenAiChatClient : ILlmChatClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;

    public RunPodOpenAiChatClient(HttpClient http, string apiKey, string model)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _model = string.IsNullOrWhiteSpace(model) ? "default" : model.Trim();
    }

    public async Task<LlmCompletion> CompleteAsync(
        LlmRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var messages = new List<object>(1 + request.Messages.Count)
        {
            new { role = "system", content = request.SystemPrompt },
        };
        foreach (var message in request.Messages)
        {
            messages.Add(new { role = message.Role, content = message.Content });
        }

        var body = new Dictionary<string, object?>
        {
            ["model"] = _model,
            ["messages"] = messages,
            ["temperature"] = request.Temperature,
        };
        if (request.MaxTokens is int maxTokens)
        {
            body["max_tokens"] = maxTokens;
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(body, options: JsonOptions),
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        using var response = await _http.SendAsync(httpRequest, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"RunPod chat completion failed with {(int)response.StatusCode} {response.StatusCode}: {Truncate(responseText, 500)}");
        }

        using var doc = JsonDocument.Parse(responseText);
        var root = doc.RootElement;
        if (!root.TryGetProperty("choices", out var choices)
            || choices.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("RunPod response contained no choices.");
        }

        var choice = choices[0];
        var content = choice.GetProperty("message").GetProperty("content").GetString()
            ?? throw new InvalidOperationException("RunPod response message content was null.");
        var finishReason = choice.TryGetProperty("finish_reason", out var fr)
            ? fr.GetString()
            : null;

        return new LlmCompletion(content, finishReason);
    }

    /// <summary>
    /// Prefer <paramref name="openAiBaseUrl"/>; otherwise build from
    /// <c>https://api.runpod.ai/v2/{chatEndpointId}/openai/v1/</c>.
    /// </summary>
    public static string ResolveBaseUrl(string? openAiBaseUrl, string? chatEndpointId)
    {
        if (!string.IsNullOrWhiteSpace(openAiBaseUrl))
        {
            return EnsureTrailingSlash(openAiBaseUrl.Trim());
        }

        if (string.IsNullOrWhiteSpace(chatEndpointId))
        {
            throw new InvalidOperationException(
                "Set RUNPOD_OPENAI_BASE_URL or RUNPOD_CHAT_ENDPOINT_ID when RUNPOD_API_KEY is configured.");
        }

        return $"https://api.runpod.ai/v2/{chatEndpointId.Trim()}/openai/v1/";
    }

    private static string EnsureTrailingSlash(string url) =>
        url.EndsWith('/') ? url : url + "/";

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
