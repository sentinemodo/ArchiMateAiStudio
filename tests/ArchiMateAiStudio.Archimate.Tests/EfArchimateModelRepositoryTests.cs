using ArchiMateAiStudio.Infrastructure.Persistence.Ef;
using Microsoft.EntityFrameworkCore;

namespace ArchiMateAiStudio.Archimate.Tests;

public class EfArchimateModelRepositoryTests
{
    private static StudioDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new StudioDbContext(options);
    }

    [Fact]
    public async Task Create_ThenGet_ReturnsStoredXmlAndMetadata()
    {
        await using var context = CreateContext();
        var repository = new EfArchimateModelRepository(context);
        const string xml =
            """<?xml version="1.0"?><archimate:model xmlns:archimate="http://www.archimatetool.com/archimate" name="Demo" id="id-1"/>""";

        var created = await repository.CreateAsync("Demo", xml);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("Demo", created.Name);
        Assert.Equal(xml, created.ArchimateXml);

        var loaded = await repository.GetAsync(created.Id);
        Assert.NotNull(loaded);
        Assert.Equal(created.Id, loaded.Id);
        Assert.Equal(xml, loaded.ArchimateXml);
    }

    [Fact]
    public async Task Save_UpdatesXmlAndName()
    {
        await using var context = CreateContext();
        var repository = new EfArchimateModelRepository(context);
        var created = await repository.CreateAsync("Old", "<old/>");

        var saved = await repository.SaveAsync(created.Id, "New", "<new/>");

        Assert.NotNull(saved);
        Assert.Equal("New", saved.Name);
        Assert.Equal("<new/>", saved.ArchimateXml);
        Assert.True(saved.UpdatedAt >= created.UpdatedAt);
    }

    [Fact]
    public async Task List_OrdersByUpdatedAtDescending()
    {
        await using var context = CreateContext();
        var repository = new EfArchimateModelRepository(context);
        var first = await repository.CreateAsync("A", "<a/>");
        await Task.Delay(5);
        var second = await repository.CreateAsync("B", "<b/>");

        var items = await repository.ListAsync();

        Assert.Equal(2, items.Count);
        Assert.Equal(second.Id, items[0].Id);
        Assert.Equal(first.Id, items[1].Id);
    }
}
