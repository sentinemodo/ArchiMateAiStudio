using ArchiMateAiStudio.Application.Generate;
using ArchiMateAiStudio.Application.Ingest;
using ArchiMateAiStudio.Application.Proposals;
using Microsoft.Extensions.DependencyInjection;

namespace ArchiMateAiStudio.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<ChangeProposalService>();
        services.AddSingleton<GenerateModelChangesService>();
        services.AddSingleton<PdfIngestService>();
        // TODO(architecture): modules-and-integrations — register MediatR handlers per module.
        return services;
    }
}
