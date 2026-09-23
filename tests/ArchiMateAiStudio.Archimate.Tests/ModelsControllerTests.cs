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

public class ModelsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ModelsControllerTests(WebApplicationFactory<Program> factory)
    {
        var customized = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var existing = services
                    .Where(d => d.ServiceType == typeof(IArchimateModelRepository))
                    .ToList();
                foreach (var descriptor in existing)
                {
                    services.Remove(descriptor);
                }

                services.AddSingleton<IArchimateModelRepository, InMemoryArchimateModelRepository>();
            });
        });

        _client = customized.CreateClient();
    }

    [Fact]
    public async Task Post_CreateEmpty_ThenList_ContainsModel()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/models",
            new { name = "Empty Demo" });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();
        Assert.Equal("Empty Demo", created.GetProperty("name").GetString());

        var listResponse = await _client.GetAsync("/api/v1/models");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var items = list.GetProperty("items").EnumerateArray().ToList();
        Assert.Contains(items, item => item.GetProperty("id").GetGuid() == id);
    }

    [Fact]
    public async Task Post_ImportArchimate_ThenGet_ReturnsDigest()
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
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();
        Assert.Equal("Minimal", created.GetProperty("name").GetString());

        var getResponse = await _client.GetAsync($"/api/v1/models/{id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var detail = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(id, detail.GetProperty("id").GetGuid());
        Assert.Equal("Minimal", detail.GetProperty("name").GetString());
        Assert.Contains("Elements: 2", detail.GetProperty("digest").GetString());
        Assert.Equal(2, detail.GetProperty("stats").GetProperty("elements").GetInt32());
        Assert.Equal(1, detail.GetProperty("stats").GetProperty("relationships").GetInt32());
    }

    [Fact]
    public async Task Get_Export_ReturnsArchimateXml()
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "fixtures",
            "minimal",
            "minimal.archimate");
        var bytes = await File.ReadAllBytesAsync(fixturePath);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(bytes), "file", "minimal.archimate");

        var createResponse = await _client.PostAsync("/api/v1/models", content);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();

        var exportResponse = await _client.GetAsync($"/api/v1/models/{id}/export");
        Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);
        Assert.Equal("application/xml", exportResponse.Content.Headers.ContentType?.MediaType);

        var xml = await exportResponse.Content.ReadAsStringAsync();
        Assert.Contains("archimate:model", xml);
        Assert.Contains("Customer", xml);
    }

    [Fact]
    public async Task Get_UnknownModel_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/models/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
