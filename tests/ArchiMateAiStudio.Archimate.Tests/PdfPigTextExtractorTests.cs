using ArchiMateAiStudio.Infrastructure.Ingestion;

namespace ArchiMateAiStudio.Archimate.Tests;

public class PdfPigTextExtractorTests
{
    [Fact]
    public void ExtractText_FromSampleClaimsPdf_ContainsClaimsPortal()
    {
        var bytes = File.ReadAllBytes(FixturePath("sample-claims.pdf"));
        var extractor = new PdfPigTextExtractor();

        var text = extractor.ExtractText(bytes);

        Assert.Contains("Claims Portal", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ApplicationComponent", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExtractText_FromSampleBillingPdf_ContainsBillingEngine()
    {
        var bytes = File.ReadAllBytes(FixturePath("sample-billing.pdf"));
        var extractor = new PdfPigTextExtractor();

        var text = extractor.ExtractText(bytes);

        Assert.Contains("Billing Engine", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExtractText_EmptyBytes_Throws()
    {
        var extractor = new PdfPigTextExtractor();
        Assert.ThrowsAny<Exception>(() => extractor.ExtractText([]));
    }

    private static string FixturePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "fixtures", "pdf", fileName);
}
