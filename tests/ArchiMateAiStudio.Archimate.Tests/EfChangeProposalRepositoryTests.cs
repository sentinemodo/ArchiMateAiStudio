using ArchiMateAiStudio.Domain.Patches;
using ArchiMateAiStudio.Domain.Proposals;
using ArchiMateAiStudio.Infrastructure.Persistence.Ef;
using Microsoft.EntityFrameworkCore;

namespace ArchiMateAiStudio.Archimate.Tests;

public class EfChangeProposalRepositoryTests
{
    private static StudioDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new StudioDbContext(options);
    }

    [Fact]
    public async Task Create_ThenGet_RoundTripsPatchJson()
    {
        await using var context = CreateContext();
        var repository = new EfChangeProposalRepository(context);
        var proposal = new ChangeProposal
        {
            Id = Guid.NewGuid(),
            ModelId = Guid.NewGuid(),
            Source = "manual",
            Status = ChangeProposalStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            Patch = new ModelPatch
            {
                Elements =
                [
                    new ElementPatch
                    {
                        Type = "ApplicationComponent",
                        Name = "Claims Portal",
                        Id = "id-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                    },
                ],
            },
        };

        await repository.CreateAsync(proposal);
        var loaded = await repository.GetAsync(proposal.Id);

        Assert.NotNull(loaded);
        Assert.Equal(proposal.Id, loaded.Id);
        Assert.Equal(proposal.ModelId, loaded.ModelId);
        Assert.Equal("manual", loaded.Source);
        Assert.Equal(ChangeProposalStatus.Pending, loaded.Status);
        Assert.Single(loaded.Patch.Elements);
        Assert.Equal("Claims Portal", loaded.Patch.Elements[0].Name);
        Assert.Equal("ApplicationComponent", loaded.Patch.Elements[0].Type);
    }

    [Fact]
    public async Task UpdateStatus_PersistsNewStatus()
    {
        await using var context = CreateContext();
        var repository = new EfChangeProposalRepository(context);
        var proposal = new ChangeProposal
        {
            Id = Guid.NewGuid(),
            ModelId = Guid.NewGuid(),
            Source = "manual",
            Status = ChangeProposalStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            Patch = new ModelPatch(),
        };
        await repository.CreateAsync(proposal);

        var updated = await repository.UpdateStatusAsync(proposal.Id, ChangeProposalStatus.Approved);

        Assert.NotNull(updated);
        Assert.Equal(ChangeProposalStatus.Approved, updated.Status);
        var loaded = await repository.GetAsync(proposal.Id);
        Assert.Equal(ChangeProposalStatus.Approved, loaded!.Status);
    }

    [Fact]
    public async Task List_FiltersByModelId()
    {
        await using var context = CreateContext();
        var repository = new EfChangeProposalRepository(context);
        var modelA = Guid.NewGuid();
        var modelB = Guid.NewGuid();
        await repository.CreateAsync(new ChangeProposal
        {
            Id = Guid.NewGuid(),
            ModelId = modelA,
            Source = "a",
            Patch = new ModelPatch(),
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await repository.CreateAsync(new ChangeProposal
        {
            Id = Guid.NewGuid(),
            ModelId = modelB,
            Source = "b",
            Patch = new ModelPatch(),
            CreatedAt = DateTimeOffset.UtcNow,
        });

        var items = await repository.ListAsync(modelA);

        Assert.Single(items);
        Assert.Equal(modelA, items[0].ModelId);
    }
}
