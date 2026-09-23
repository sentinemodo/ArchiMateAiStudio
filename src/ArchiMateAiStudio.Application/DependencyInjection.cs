using ArchiMateAiStudio.Application.Generate;
using ArchiMateAiStudio.Application.Ingest;
using ArchiMateAiStudio.Application.Proposals;
using ArchiMateAiStudio.Application.Rag;
using Microsoft.Extensions.DependencyInjection;

namespace ArchiMateAiStudio.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Scoped so EF-backed repositories / RAG indexes resolve correctly per request.
        services.AddScoped<ChangeProposalService>();
        services.AddScoped<GenerateModelChangesService>();
        services.AddScoped<PdfIngestService>();
        services.AddScoped<ModelRagIndexer>();
        // TODO(architecture): modules-and-integrations — register MediatR handlers per module.
        return services;
    }
}
