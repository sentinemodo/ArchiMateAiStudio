using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ArchiMateAiStudio.Domain.Ports;
using ArchiMateAiStudio.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace ArchiMateAiStudio.Archimate.Tests;

public class ProposalsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProposalsControllerTests(WebApplicationFactory<Program> factory)
    {
        var customized = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                ReplaceSingleton<IArchimateModelRepository, InMemoryArchimateModelRepository>(services);
                ReplaceSingleton<IChangeProposalRepository, InMemoryChangeProposalRepository>(services);
            });
        });

        _client = customized.CreateClient();
    }

    [Fact]
    public async Task Approve_AppliesPatch_ExportContainsElement()
    {
        var modelId = await ImportMinimalAsync();

        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/proposals",
            new
            {
                modelId,
                source = "manual",
                patch = new
                {
                    elements = new[]
                    {
                        new
                        {
                            type = "ApplicationComponent",
                            name = "Claims Portal",
                            id = "id-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                        },
                    },
                },
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var proposalId = created.GetProperty("id").GetGuid();
        Assert.Equal("Pending", created.GetProperty("status").GetString());

        var approveResponse = await _client.PostAsync($"/api/v1/proposals/{proposalId}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        var approved = await approveResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Approved", approved.GetProperty("status").GetString());

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
    public async Task Reject_DoesNotChangeModel()
    {
        var modelId = await ImportMinimalAsync();

        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/proposals",
            new
            {
                modelId,
                source = "manual",
                patch = new
                {
                    elements = new[]
                    {
                        new { type = "ApplicationComponent", name = "Should Not Appear" },
                    },
                },
            });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var proposalId = created.GetProperty("id").GetGuid();

        var rejectResponse = await _client.PostAsync($"/api/v1/proposals/{proposalId}/reject", null);
        Assert.Equal(HttpStatusCode.OK, rejectResponse.StatusCode);
        var rejected = await rejectResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Rejected", rejected.GetProperty("status").GetString());

        var exportResponse = await _client.GetAsync($"/api/v1/models/{modelId}/export");
        var xml = await exportResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Should Not Appear", xml);

        var getResponse = await _client.GetAsync($"/api/v1/models/{modelId}");
        var detail = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, detail.GetProperty("stats").GetProperty("elements").GetInt32());
    }

    [Fact]
    public async Task Approve_InvalidPatch_Returns400_ModelUnchanged()
    {
        var modelId = await ImportMinimalAsync();

        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/proposals",
            new
            {
                modelId,
                source = "manual",
                patch = new
                {
                    elements = new[]
                    {
                        new { type = "NotARealType", name = "Bad Element" },
                    },
                },
            });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var proposalId = created.GetProperty("id").GetGuid();

        var approveResponse = await _client.PostAsync($"/api/v1/proposals/{proposalId}/approve", null);
        Assert.Equal(HttpStatusCode.BadRequest, approveResponse.StatusCode);
        var body = await approveResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("errors", out var errors));
        Assert.NotEmpty(errors.EnumerateArray().ToList());

        var exportResponse = await _client.GetAsync($"/api/v1/models/{modelId}/export");
        var xml = await exportResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Bad Element", xml);

        var listResponse = await _client.GetAsync($"/api/v1/proposals?modelId={modelId}");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var items = list.GetProperty("items").EnumerateArray().ToList();
        var proposal = Assert.Single(items, i => i.GetProperty("id").GetGuid() == proposalId);
        Assert.Equal("Pending", proposal.GetProperty("status").GetString());
    }

    [Fact]
    public async Task List_FiltersByModelId()
    {
        var modelA = await ImportMinimalAsync();
        var modelB = await ImportMinimalAsync();

        await _client.PostAsJsonAsync(
            "/api/v1/proposals",
            new
            {
                modelId = modelA,
                patch = new { elements = new[] { new { type = "ApplicationComponent", name = "A" } } },
            });
        await _client.PostAsJsonAsync(
            "/api/v1/proposals",
            new
            {
                modelId = modelB,
                patch = new { elements = new[] { new { type = "ApplicationComponent", name = "B" } } },
            });

        var listResponse = await _client.GetAsync($"/api/v1/proposals?modelId={modelA}");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var items = list.GetProperty("items").EnumerateArray().ToList();
        Assert.All(items, item => Assert.Equal(modelA, item.GetProperty("modelId").GetGuid()));
        Assert.Contains(items, item => item.GetProperty("status").GetString() == "Pending");
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
