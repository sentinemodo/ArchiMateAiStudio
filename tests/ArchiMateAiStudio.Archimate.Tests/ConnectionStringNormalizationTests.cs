using ArchiMateAiStudio.Infrastructure;

namespace ArchiMateAiStudio.Archimate.Tests;

public class ConnectionStringNormalizationTests
{
    [Fact]
    public void NormalizeConnectionString_ConvertsPostgresUri()
    {
        const string uri =
            "postgresql://user:p%40ss@db.example.com:5432/mydb?sslmode=require&channel_binding=require";

        var normalized = DependencyInjection.NormalizeConnectionString(uri);

        Assert.Contains("Host=db.example.com", normalized);
        Assert.Contains("Port=5432", normalized);
        Assert.Contains("Database=mydb", normalized);
        Assert.Contains("Username=user", normalized);
        Assert.Contains("Password=p@ss", normalized);
        Assert.Contains("SSL Mode=Require", normalized);
        Assert.DoesNotContain("postgresql://", normalized);
    }

    [Fact]
    public void NormalizeConnectionString_LeavesKeywordFormatUnchanged()
    {
        const string keyword =
            "Host=127.0.0.1;Port=5432;Database=archimate;Username=postgres;Password=secret";

        Assert.Equal(keyword, DependencyInjection.NormalizeConnectionString(keyword));
    }
}
