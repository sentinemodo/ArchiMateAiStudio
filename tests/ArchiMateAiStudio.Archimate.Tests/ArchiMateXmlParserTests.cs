using ArchiMateAiStudio.Archimate.Comparison;
using ArchiMateAiStudio.Archimate.Parsing;
using ArchiMateAiStudio.Archimate.Serialization;

namespace ArchiMateAiStudio.Archimate.Tests;

public class ArchiMateXmlParserTests
{
    private static string MinimalFixturePath =>
        Path.Combine(AppContext.BaseDirectory, "fixtures", "minimal", "minimal.archimate");

    [Fact]
    public void Parse_MinimalFixture_LoadsElementsAndRelationships()
    {
        var document = ArchiMateXmlParser.ParseFile(MinimalFixturePath);

        Assert.Equal("Minimal", document.Name);
        Assert.Equal(2, document.Statistics.ElementCount);
        Assert.Equal(1, document.Statistics.RelationshipCount);
    }

    [Fact]
    public void RoundTrip_MinimalFixture_PreservesConcepts()
    {
        var original = ArchiMateXmlParser.ParseFile(MinimalFixturePath);
        var xml = ArchiMateXmlSerializer.Serialize(original);
        var roundTripped = ArchiMateXmlParser.Parse(xml);

        var comparison = ArchimateDocumentComparer.Compare(original, roundTripped);

        Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences));
    }
}
