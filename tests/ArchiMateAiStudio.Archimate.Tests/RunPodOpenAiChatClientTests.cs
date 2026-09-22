using System.Net;
using System.Text;
using System.Text.Json;
using ArchiMateAiStudio.Domain.Ports;
using ArchiMateAiStudio.Infrastructure.Llm;

namespace ArchiMateAiStudio.Archimate.Tests;

public class RunPodOpenAiChatClientTests
{
    [Fact]
    public async Task CompleteAsync_PostsOpenAiCompatibleChatCompletion_AndReturnsAssistantContent()
    {
        string? capturedUrl = null;
        string? capturedAuth = null;
        string? capturedBody = null;

        var handler = new CapturingHandler(async (request, _) =>
        {
            capturedUrl = request.RequestUri?.ToString();
            capturedAuth = request.Headers.Authorization?.ToString();
            capturedBody = await request.Content!.ReadAsStringAsync();

            var responseJson = """
                {
                  "id": "chatcmpl-test",
                  "choices": [
                    {
                      "index": 0,
                      "message": { "role": "assistant", "content": "{\"elements\":[]}" },
                      "finish_reason": "stop"
                    }
                  ]
                }
                """;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
            };
        });

        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.runpod.ai/v2/endpoint-abc/openai/v1/"),
        };

        var client = new RunPodOpenAiChatClient(
            http,
            apiKey: "test-key-not-real",
            model: "qwen2.5-72b-instruct");

        var completion = await client.CompleteAsync(
            new LlmRequest(
                "You are a helpful assistant.",
                [new LlmMessage("user", "Return a ModelPatch.")],
                MaxTokens: 256,
                Temperature: 0.1f));

        Assert.Equal("{\"elements\":[]}", completion.Content);
        Assert.Equal("stop", completion.FinishReason);
        Assert.Equal(
            "https://api.runpod.ai/v2/endpoint-abc/openai/v1/chat/completions",
            capturedUrl);
        Assert.Equal("Bearer test-key-not-real", capturedAuth);

        using var doc = JsonDocument.Parse(capturedBody!);
        var root = doc.RootElement;
        Assert.Equal("qwen2.5-72b-instruct", root.GetProperty("model").GetString());
        Assert.Equal(0.1f, root.GetProperty("temperature").GetSingle(), precision: 3);
        Assert.Equal(256, root.GetProperty("max_tokens").GetInt32());

        var messages = root.GetProperty("messages").EnumerateArray().ToList();
        Assert.Equal(2, messages.Count);
        Assert.Equal("system", messages[0].GetProperty("role").GetString());
        Assert.Equal("You are a helpful assistant.", messages[0].GetProperty("content").GetString());
        Assert.Equal("user", messages[1].GetProperty("role").GetString());
        Assert.Equal("Return a ModelPatch.", messages[1].GetProperty("content").GetString());
    }

    [Fact]
    public async Task CompleteAsync_WhenHttpError_ThrowsHttpRequestException()
    {
        var handler = new CapturingHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("""{"error":"bad key"}""", Encoding.UTF8, "application/json"),
            }));

        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.runpod.ai/v2/ep/openai/v1/"),
        };

        var client = new RunPodOpenAiChatClient(http, apiKey: "bad", model: "m");

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.CompleteAsync(new LlmRequest("sys", [new LlmMessage("user", "hi")])));
    }

    [Fact]
    public void ResolveBaseUrl_PrefersExplicitOpenAiBaseUrl()
    {
        var url = RunPodOpenAiChatClient.ResolveBaseUrl(
            openAiBaseUrl: "https://api.runpod.ai/v2/custom/openai/v1",
            chatEndpointId: "ignored-endpoint");

        Assert.Equal("https://api.runpod.ai/v2/custom/openai/v1/", url);
    }

    [Fact]
    public void ResolveBaseUrl_BuildsFromChatEndpointId()
    {
        var url = RunPodOpenAiChatClient.ResolveBaseUrl(
            openAiBaseUrl: null,
            chatEndpointId: "abc123");

        Assert.Equal("https://api.runpod.ai/v2/abc123/openai/v1/", url);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public CapturingHandler(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            _handler(request, cancellationToken);
    }
}
