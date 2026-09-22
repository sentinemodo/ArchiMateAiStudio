using ArchiMateAiStudio.Domain.Patches;
using ArchiMateAiStudio.Domain.Proposals;
using ArchiMateAiStudio.Infrastructure;
using ArchiMateAiStudio.Infrastructure.Persistence.Ef;
using Microsoft.EntityFrameworkCore;

namespace ArchiMateAiStudio.Archimate.Tests;

/// <summary>
/// Optional live Postgres smoke. Skips unless DATABASE_URL is set in the environment.
/// </summary>
public class PostgresPersistenceIntegrationTests
{
    [Fact]
    public async Task CreateModelAndProposal_AgainstLivePostgres_WhenDatabaseUrlSet()
    {
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return; // skip when no live DB (CI default)
        }

        var options = new DbContextOptionsBuilder<StudioDbContext>()
            .UseNpgsql(DependencyInjection.NormalizeConnectionString(connectionString))
            .Options;

        await using var context = new StudioDbContext(options);
        await context.Database.MigrateAsync();

        var models = new EfArchimateModelRepository(context);
        var proposals = new EfChangeProposalRepository(context);

        var model = await models.CreateAsync(
            $"pg-smoke-{Guid.NewGuid():N}",
            """<?xml version="1.0"?><archimate:model xmlns:archimate="http://www.archimatetool.com/archimate" name="Smoke" id="id-1"/>""");

        var proposal = await proposals.CreateAsync(new ChangeProposal
        {
            Id = Guid.NewGuid(),
            ModelId = model.Id,
            Source = "pg-smoke",
            Patch = new ModelPatch
            {
                Elements =
                [
                    new ElementPatch { Type = "BusinessActor", Name = "Smoke Actor" },
                ],
            },
            CreatedAt = DateTimeOffset.UtcNow,
        });

        var loadedModel = await models.GetAsync(model.Id);
        var loadedProposal = await proposals.GetAsync(proposal.Id);

        Assert.NotNull(loadedModel);
        Assert.Equal(model.Name, loadedModel.Name);
        Assert.NotNull(loadedProposal);
        Assert.Equal("pg-smoke", loadedProposal.Source);
        Assert.Single(loadedProposal.Patch.Elements);
    }
}
