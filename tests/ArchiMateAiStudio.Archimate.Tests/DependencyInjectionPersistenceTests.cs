using ArchiMateAiStudio.Domain.Ports;
using ArchiMateAiStudio.Infrastructure;
using ArchiMateAiStudio.Infrastructure.Llm;
using ArchiMateAiStudio.Infrastructure.Persistence;
using ArchiMateAiStudio.Infrastructure.Persistence.Ef;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArchiMateAiStudio.Archimate.Tests;

public class DependencyInjectionPersistenceTests
{
    [Fact]
    public void AddInfrastructure_WithoutConnection_RegistersInMemoryRepositories()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        Assert.IsType<InMemoryArchimateModelRepository>(
            provider.GetRequiredService<IArchimateModelRepository>());
        Assert.IsType<InMemoryChangeProposalRepository>(
            provider.GetRequiredService<IChangeProposalRepository>());
        Assert.Null(provider.GetService<StudioDbContext>());
        Assert.IsType<StubLlmChatClient>(provider.GetRequiredService<ILlmChatClient>());
    }

    [Fact]
    public void AddInfrastructure_WithRunPodApiKey_RegistersRunPodOpenAiChatClient()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RUNPOD_API_KEY"] = "test-key-not-real",
                ["RUNPOD_CHAT_ENDPOINT_ID"] = "endpoint-xyz",
                ["LLM_CHAT_MODEL"] = "qwen2.5-72b-instruct",
            })
            .Build();

        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        Assert.IsType<RunPodOpenAiChatClient>(provider.GetRequiredService<ILlmChatClient>());
    }

    [Fact]
    public void AddInfrastructure_WithConnectionStringsDefault_RegistersEfRepositories()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] =
                    "Host=127.0.0.1;Port=5432;Database=archimate_ai_studio_test;Username=postgres;Password=postgres",
            })
            .Build();

        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        Assert.IsType<EfArchimateModelRepository>(
            provider.GetRequiredService<IArchimateModelRepository>());
        Assert.IsType<EfChangeProposalRepository>(
            provider.GetRequiredService<IChangeProposalRepository>());
        Assert.NotNull(provider.GetService<StudioDbContext>());
    }
}
