using ArchiMateAiStudio.Domain.Ports;
using ArchiMateAiStudio.Infrastructure.Llm;
using ArchiMateAiStudio.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace ArchiMateAiStudio.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ILlmChatClient, StubLlmChatClient>();
        services.AddSingleton<IArchimateModelRepository, InMemoryArchimateModelRepository>();
        services.AddSingleton<IChangeProposalRepository, InMemoryChangeProposalRepository>();
        return services;
    }
}
