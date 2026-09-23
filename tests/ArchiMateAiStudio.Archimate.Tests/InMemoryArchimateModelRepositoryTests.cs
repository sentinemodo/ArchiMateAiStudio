using ArchiMateAiStudio.Domain.Models;
using ArchiMateAiStudio.Domain.Ports;
using ArchiMateAiStudio.Infrastructure.Persistence;

namespace ArchiMateAiStudio.Archimate.Tests;

public class InMemoryArchimateModelRepositoryTests
{
    private readonly IArchimateModelRepository _repository = new InMemoryArchimateModelRepository();

    [Fact]
    public async Task Create_ThenGet_ReturnsStoredXmlAndMetadata()
    {
        const string xml = """<?xml version="1.0"?><archimate:model xmlns:archimate="http://www.archimatetool.com/archimate" name="Demo" id="id-1"/>""";

        var created = await _repository.CreateAsync("Demo", xml);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("Demo", created.Name);
        Assert.Equal(xml, created.ArchimateXml);
        Assert.True(created.UpdatedAt <= DateTimeOffset.UtcNow);

        var loaded = await _repository.GetAsync(created.Id);
        Assert.NotNull(loaded);
        Assert.Equal(created.Id, loaded.Id);
        Assert.Equal(xml, loaded.ArchimateXml);
    }

    [Fact]
    public async Task List_ReturnsAllCreatedModels()
    {
        await _repository.CreateAsync("A", "<a/>");
        await _repository.CreateAsync("B", "<b/>");

        var items = await _repository.ListAsync();

        Assert.Equal(2, items.Count);
        Assert.Contains(items, m => m.Name == "A");
        Assert.Contains(items, m => m.Name == "B");
    }

    [Fact]
    public async Task Save_UpdatesXmlAndName()
    {
        var created = await _repository.CreateAsync("Old", "<old/>");

        var saved = await _repository.SaveAsync(created.Id, "New", "<new/>");

        Assert.NotNull(saved);
        Assert.Equal("New", saved.Name);
        Assert.Equal("<new/>", saved.ArchimateXml);
        Assert.True(saved.UpdatedAt >= created.UpdatedAt);
    }

    [Fact]
    public async Task Get_UnknownId_ReturnsNull()
    {
        var loaded = await _repository.GetAsync(Guid.NewGuid());
        Assert.Null(loaded);
    }

    [Fact]
    public async Task Exists_ReflectsPresence()
    {
        var created = await _repository.CreateAsync("X", "<x/>");

        Assert.True(await _repository.ExistsAsync(created.Id));
        Assert.False(await _repository.ExistsAsync(Guid.NewGuid()));
    }
}
