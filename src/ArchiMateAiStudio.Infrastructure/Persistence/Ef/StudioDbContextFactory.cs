using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ArchiMateAiStudio.Infrastructure.Persistence.Ef;

/// <summary>
/// Design-time factory for <c>dotnet ef migrations</c>. Uses DATABASE_URL or a local placeholder.
/// </summary>
public sealed class StudioDbContextFactory : IDesignTimeDbContextFactory<StudioDbContext>
{
    public StudioDbContext CreateDbContext(string[] args)
    {
        var connectionString = DependencyInjection.ResolveConnectionString(configuration: null)
            ?? "Host=127.0.0.1;Port=5432;Database=archimate_ai_studio;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<StudioDbContext>()
            .UseNpgsql(DependencyInjection.NormalizeConnectionString(connectionString))
            .Options;

        return new StudioDbContext(options);
    }
}
