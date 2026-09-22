using ArchiMateAiStudio.Domain.Ports;
using ArchiMateAiStudio.Infrastructure.Ingestion;
using ArchiMateAiStudio.Infrastructure.Llm;
using ArchiMateAiStudio.Infrastructure.Persistence;
using ArchiMateAiStudio.Infrastructure.Persistence.Ef;
using ArchiMateAiStudio.Infrastructure.Rag;
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
        RegisterLlmClient(services, configuration);
        services.AddSingleton<IEmbeddingClient, StubEmbeddingClient>();

        var connectionString = ResolveConnectionString(configuration);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<StudioDbContext>(options =>
                options.UseNpgsql(NormalizeConnectionString(connectionString)));
            services.AddScoped<IArchimateModelRepository, EfArchimateModelRepository>();
            services.AddScoped<IChangeProposalRepository, EfChangeProposalRepository>();
            // Cosine similarity in C# (embedding stored as float[] JSON). pgvector optional later.
            services.AddScoped<IRagIndex, EfRagIndex>();
        }
        else
        {
            services.AddSingleton<IArchimateModelRepository, InMemoryArchimateModelRepository>();
            services.AddSingleton<IChangeProposalRepository, InMemoryChangeProposalRepository>();
            services.AddSingleton<IRagIndex, InMemoryRagIndex>();
        }

        return services;
    }

    /// <summary>
    /// When <c>RUNPOD_API_KEY</c> is set, registers <see cref="RunPodOpenAiChatClient"/>;
    /// otherwise <see cref="StubLlmChatClient"/> (CI / offline default).
    /// </summary>
    public static void RegisterLlmClient(IServiceCollection services, IConfiguration? configuration)
    {
        var apiKey = FirstNonEmpty(
            configuration?["RUNPOD_API_KEY"],
            Environment.GetEnvironmentVariable("RUNPOD_API_KEY"));

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            services.AddSingleton<ILlmChatClient, StubLlmChatClient>();
        }
        else
        {
            var openAiBaseUrl = FirstNonEmpty(
                configuration?["RUNPOD_OPENAI_BASE_URL"],
                Environment.GetEnvironmentVariable("RUNPOD_OPENAI_BASE_URL"));
            var endpointId = FirstNonEmpty(
                configuration?["RUNPOD_CHAT_ENDPOINT_ID"],
                Environment.GetEnvironmentVariable("RUNPOD_CHAT_ENDPOINT_ID"),
                configuration?["RUNPOD_ENDPOINT_ID"],
                Environment.GetEnvironmentVariable("RUNPOD_ENDPOINT_ID"));
            var model = FirstNonEmpty(
                configuration?["LLM_CHAT_MODEL"],
                Environment.GetEnvironmentVariable("LLM_CHAT_MODEL"),
                configuration?["RUNPOD_CHAT_MODEL"],
                Environment.GetEnvironmentVariable("RUNPOD_CHAT_MODEL")) ?? "default";

            var baseUrl = RunPodOpenAiChatClient.ResolveBaseUrl(openAiBaseUrl, endpointId);

            services.AddHttpClient(nameof(RunPodOpenAiChatClient), client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromMinutes(2);
            });

            services.AddSingleton<ILlmChatClient>(sp =>
            {
                var http = sp.GetRequiredService<IHttpClientFactory>()
                    .CreateClient(nameof(RunPodOpenAiChatClient));
                return new RunPodOpenAiChatClient(http, apiKey, model);
            });
        }

        services.AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>();
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
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
