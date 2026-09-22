using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ArchiMateAiStudio.Domain.Ports;
using ArchiMateAiStudio.Infrastructure.Llm;
using ArchiMateAiStudio.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace ArchiMateAiStudio.Archimate.Tests;

public class GenerateControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GenerateControllerTests(WebApplicationFactory<Program> factory)
    {
        var customized = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                ReplaceSingleton<IArchimateModelRepository, InMemoryArchimateModelRepository>(services);
                ReplaceSingleton<IChangeProposalRepository, InMemoryChangeProposalRepository>(services);
                ReplaceSingleton<ILlmChatClient, StubLlmChatClient>(services);
            });
        });

        _client = customized.CreateClient();
    }

    [Fact]
    public async Task Generate_CreatesPendingProposal_WithSourceGenerate()
    {
        var modelId = await ImportMinimalAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/models/{modelId}/generate",
            new { instruction = "Add ApplicationComponent Claims Portal for claims intake." });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var proposalId = body.GetProperty("proposalId").GetGuid();
        Assert.NotEqual(Guid.Empty, proposalId);

        var summary = body.GetProperty("summary");
        Assert.True(summary.GetProperty("elementCount").GetInt32() >= 1);
        Assert.Contains(
            "Claims Portal",
            summary.GetProperty("elementNames").EnumerateArray().Select(e => e.GetString()));

        var listResponse = await _client.GetAsync($"/api/v1/proposals?modelId={modelId}");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var items = list.GetProperty("items").EnumerateArray().ToList();
        var proposal = Assert.Single(items, i => i.GetProperty("id").GetGuid() == proposalId);
        Assert.Equal("Pending", proposal.GetProperty("status").GetString());
        Assert.Equal("generate", proposal.GetProperty("source").GetString());
    }

    [Fact]
    public async Task Approve_AfterGenerate_AddsElementToModel()
    {
        var modelId = await ImportMinimalAsync();

        var generateResponse = await _client.PostAsJsonAsync(
            $"/api/v1/models/{modelId}/generate",
            new { instruction = "Add ApplicationComponent named Claims Portal." });
        Assert.Equal(HttpStatusCode.OK, generateResponse.StatusCode);
        var generated = await generateResponse.Content.ReadFromJsonAsync<JsonElement>();
        var proposalId = generated.GetProperty("proposalId").GetGuid();

        var approveResponse = await _client.PostAsync($"/api/v1/proposals/{proposalId}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        var exportResponse = await _client.GetAsync($"/api/v1/models/{modelId}/export");
        Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);
        var xml = await exportResponse.Content.ReadAsStringAsync();
        Assert.Contains("Claims Portal", xml);
        Assert.Contains("ApplicationComponent", xml);

        var getResponse = await _client.GetAsync($"/api/v1/models/{modelId}");
        var detail = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, detail.GetProperty("stats").GetProperty("elements").GetInt32());
    }

    [Fact]
    public async Task Generate_UnknownModel_ReturnsNotFound()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/models/{Guid.NewGuid()}/generate",
            new { instruction = "Add something" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Generate_EmptyInstruction_ReturnsBadRequest()
    {
        var modelId = await ImportMinimalAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/models/{modelId}/generate",
            new { instruction = "   " });

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
