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

public class IngestControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public IngestControllerTests(WebApplicationFactory<Program> factory)
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
    public async Task Ingest_PdfWithClaimsText_CreatesPendingProposal_WithSourcePdfIngest()
    {
        var modelId = await ImportMinimalAsync();
        var pdfBytes = await File.ReadAllBytesAsync(FixturePath("sample-claims.pdf"));

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "sample-claims.pdf");

        var response = await _client.PostAsync($"/api/v1/models/{modelId}/ingest", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var proposalId = body.GetProperty("proposalId").GetGuid();
        Assert.NotEqual(Guid.Empty, proposalId);
        Assert.Equal("pdf-ingest", body.GetProperty("source").GetString());
        Assert.Contains(
            "Claims Portal",
            body.GetProperty("summary").GetProperty("elementNames").EnumerateArray()
                .Select(e => e.GetString()));

        var listResponse = await _client.GetAsync($"/api/v1/proposals?modelId={modelId}");
        var list = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var proposal = Assert.Single(
            list.GetProperty("items").EnumerateArray(),
            i => i.GetProperty("id").GetGuid() == proposalId);
        Assert.Equal("Pending", proposal.GetProperty("status").GetString());
        Assert.Equal("pdf-ingest", proposal.GetProperty("source").GetString());
    }

    [Fact]
    public async Task Ingest_PdfWithoutClaimsKeywords_CreatesApplicationComponentFromText()
    {
        var modelId = await ImportMinimalAsync();
        var pdfBytes = await File.ReadAllBytesAsync(FixturePath("sample-billing.pdf"));

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "sample-billing.pdf");

        var response = await _client.PostAsync($"/api/v1/models/{modelId}/ingest", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("pdf-ingest", body.GetProperty("source").GetString());
        var names = body.GetProperty("summary").GetProperty("elementNames").EnumerateArray()
            .Select(e => e.GetString())
            .ToList();
        Assert.Contains(names, n => n is not null && n.Contains("Billing", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Ingest_UnknownModel_ReturnsNotFound()
    {
        var pdfBytes = await File.ReadAllBytesAsync(FixturePath("sample-claims.pdf"));
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "sample-claims.pdf");

        var response = await _client.PostAsync($"/api/v1/models/{Guid.NewGuid()}/ingest", content);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Ingest_MissingFile_ReturnsBadRequest()
    {
        var modelId = await ImportMinimalAsync();
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("x"), "notfile");

        var response = await _client.PostAsync($"/api/v1/models/{modelId}/ingest", content);
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

    private static string FixturePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "fixtures", "pdf", fileName);

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
