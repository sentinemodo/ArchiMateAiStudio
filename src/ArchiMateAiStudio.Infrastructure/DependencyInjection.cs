using ArchiMateAiStudio.Domain.Ports;
using ArchiMateAiStudio.Infrastructure.Llm;
using ArchiMateAiStudio.Infrastructure.Persistence;
using ArchiMateAiStudio.Infrastructure.Persistence.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArchiMateAiStudio.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services) =>
        AddInfrastructure(services, configuration: null);

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration? configuration)
    {
        services.AddSingleton<ILlmChatClient, StubLlmChatClient>();

        var connectionString = ResolveConnectionString(configuration);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<StudioDbContext>(options =>
                options.UseNpgsql(NormalizeConnectionString(connectionString)));
            services.AddScoped<IArchimateModelRepository, EfArchimateModelRepository>();
            services.AddScoped<IChangeProposalRepository, EfChangeProposalRepository>();
        }
        else
        {
            services.AddSingleton<IArchimateModelRepository, InMemoryArchimateModelRepository>();
            services.AddSingleton<IChangeProposalRepository, InMemoryChangeProposalRepository>();
        }

        return services;
    }

    /// <summary>
    /// Prefer <c>ConnectionStrings:Default</c>, then <c>DATABASE_URL</c> environment variable.
    /// </summary>
    public static string? ResolveConnectionString(IConfiguration? configuration)
    {
        var fromConfig = configuration?.GetConnectionString("Default");
        if (!string.IsNullOrWhiteSpace(fromConfig))
        {
            return fromConfig;
        }

        var fromEnv = Environment.GetEnvironmentVariable("DATABASE_URL");
        return string.IsNullOrWhiteSpace(fromEnv) ? null : fromEnv;
    }

    /// <summary>
    /// Converts postgres:// URI style DATABASE_URL values to Npgsql keyword format when needed.
    /// </summary>
    public static string NormalizeConnectionString(string connectionString)
    {
        if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var database = uri.AbsolutePath.TrimStart('/');
        var sslMode = "Require";
        var query = uri.Query.TrimStart('?');
        if (!string.IsNullOrEmpty(query))
        {
            foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = part.Split('=', 2);
                if (kv.Length == 2
                    && kv[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase))
                {
                    sslMode = MapSslMode(Uri.UnescapeDataString(kv[1]));
                }
            }
        }

        return $"Host={uri.Host};Port={(uri.Port > 0 ? uri.Port : 5432)};Database={database};Username={username};Password={password};SSL Mode={sslMode};Trust Server Certificate=true";
    }

    private static string MapSslMode(string value) => value.ToLowerInvariant() switch
    {
        "disable" => "Disable",
        "allow" => "Allow",
        "prefer" => "Prefer",
        "require" => "Require",
        "verify-ca" => "VerifyCA",
        "verify-full" => "VerifyFull",
        _ => value,
    };
}
