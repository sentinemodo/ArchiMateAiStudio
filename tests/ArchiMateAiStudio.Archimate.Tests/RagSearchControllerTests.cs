using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ArchiMateAiStudio.Domain.Ports;
using ArchiMateAiStudio.Infrastructure.Llm;
using ArchiMateAiStudio.Infrastructure.Persistence;
using ArchiMateAiStudio.Infrastructure.Rag;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace ArchiMateAiStudio.Archimate.Tests;

public class RagSearchControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public RagSearchControllerTests(WebApplicationFactory<Program> factory)
    {
        var customized = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                ReplaceSingleton<IArchimateModelRepository, InMemoryArchimateModelRepository>(services);
                ReplaceSingleton<IChangeProposalRepository, InMemoryChangeProposalRepository>(services);
                ReplaceSingleton<IEmbeddingClient, StubEmbeddingClient>(services);
                ReplaceSingleton<IRagIndex, InMemoryRagIndex>(services);
                ReplaceSingleton<ILlmChatClient, StubLlmChatClient>(services);
            });
        });

        _client = customized.CreateClient();
    }

    [Fact]
    public async Task Search_AfterImport_ReturnsElementHits()
    {
        var modelId = await ImportMinimalAsync();

        var response = await _client.GetAsync($"/api/v1/models/{modelId}/search?q=Customer");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.GetProperty("items").EnumerateArray().ToList();
        Assert.NotEmpty(items);
        Assert.Contains(
            items,
            item => (item.GetProperty("text").GetString() ?? string.Empty)
                .Contains("Customer", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Search_EmptyQuery_ReturnsBadRequest()
    {
        var modelId = await ImportMinimalAsync();
        var response = await _client.GetAsync($"/api/v1/models/{modelId}/search?q=");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<Guid> ImportMinimalAsync()
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "fixtures",
            "minimal",
            "minimal.archimate");
        var bytes = await File.ReadAllBytesAsync(fixturePath);

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
        content.Add(fileContent, "file", "minimal.archimate");

        var createResponse = await _client.PostAsync("/api/v1/models", content);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        return created.GetProperty("id").GetGuid();
    }

    private static void ReplaceSingleton<TService, TImplementation>(IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        var existing = services.Where(d => d.ServiceType == typeof(TService)).ToList();
        foreach (var descriptor in existing)
        {
            services.Remove(descriptor);
        }

        services.AddSingleton<TService, TImplementation>();
    }
}
