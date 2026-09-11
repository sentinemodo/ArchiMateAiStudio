using ArchiMateAiStudio.Archimate.Comparison;
using ArchiMateAiStudio.Archimate.Parsing;
using ArchiMateAiStudio.Archimate.Serialization;
using ArchiMateAiStudio.Archimate.Validation;

namespace ArchiMateAiStudio.Archimate.Tests;

public class ArchiSuranceRoundTripTests
{
    private static string? ResolveFixturePath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "fixtures", "archisurance", "archisurance-3.2.archimate"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "_research", "ArchiMate_ArchiSurance", "ArchiSurance2025.archimate"),
        };

        foreach (var candidate in candidates)
        {
            var fullPath = Path.GetFullPath(candidate);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }

    [Fact]
    public void RoundTrip_ArchiSurance_PreservesModelStatistics()
    {
        var fixturePath = ResolveFixturePath();
        if (fixturePath is null)
        {
            return;
        }

        var original = ArchiMateXmlParser.ParseFile(fixturePath);
        Assert.True(original.Statistics.ElementCount > 100);
        Assert.True(original.Statistics.RelationshipCount > 100);

        var xml = ArchiMateXmlSerializer.Serialize(original);
        var roundTripped = ArchiMateXmlParser.Parse(xml);

        var comparison = ArchimateDocumentComparer.Compare(original, roundTripped);

        Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Take(20)));
    }

    [Fact]
    public void Export_OpenGroupExchange_ArchiSurance_PassesXsd()
    {
        var fixturePath = ResolveFixturePath();
        if (fixturePath is null)
        {
            return;
        }

        var document = ArchiMateXmlParser.ParseFile(fixturePath);
        var exchangeXml = OpenGroupExchangeSerializer.Serialize(document);
        var result = ArchimateXsdValidator.Validate(exchangeXml);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors.Take(20)));
    }
}
