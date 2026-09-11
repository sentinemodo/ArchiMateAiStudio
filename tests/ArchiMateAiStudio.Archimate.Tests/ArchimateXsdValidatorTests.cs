using ArchiMateAiStudio.Archimate.Parsing;
using ArchiMateAiStudio.Archimate.Serialization;
using ArchiMateAiStudio.Archimate.Validation;

namespace ArchiMateAiStudio.Archimate.Tests;

public class ArchimateXsdValidatorTests
{
    private static string MinimalFixturePath =>
        Path.Combine(AppContext.BaseDirectory, "fixtures", "minimal", "minimal.archimate");

    [Fact]
    public void Validate_OpenGroupExport_MinimalFixture_PassesXsd()
    {
        var document = ArchiMateXmlParser.ParseFile(MinimalFixturePath);
        var exchangeXml = OpenGroupExchangeSerializer.Serialize(document);

        var result = ArchimateXsdValidator.Validate(exchangeXml);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
    }

    [Fact]
    public void ToExchangeIdentifier_NormalizesNumericIds()
    {
        Assert.Equal("id_6", OpenGroupExchangeSerializer.ToExchangeIdentifier("6"));
        Assert.Equal("id_abc", OpenGroupExchangeSerializer.ToExchangeIdentifier("id-abc"));
    }

    [Theory]
    [InlineData("AssignmentRelationship", "Assignment")]
    [InlineData("ServingRelationship", "Serving")]
    [InlineData("AccessRelationship", "Access")]
    public void ToExchangeRelationshipType_StripsRelationshipSuffix(string archiType, string exchangeType)
    {
        Assert.Equal(exchangeType, OpenGroupExchangeSerializer.ToExchangeRelationshipType(archiType));
    }
}
