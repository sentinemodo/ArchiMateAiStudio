using Microsoft.Extensions.DependencyInjection;

namespace ArchiMateAiStudio.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // TODO(architecture): modules-and-integrations — register MediatR handlers per module.
        return services;
    }
}
