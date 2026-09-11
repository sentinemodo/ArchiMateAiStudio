using ArchiMateAiStudio.Archimate.Validation;

namespace ArchiMateAiStudio.Archimate.Tests;

public class MetamodelValidatorTests
{
    [Theory]
    [InlineData("Actor", "BusinessActor")]
    [InlineData("TechnologyNode", "Node")]
    [InlineData("UsedBy", "ServingRelationship")]
    public void TryNormalizeElementType_MapsAliases(string raw, string expected)
    {
        var ok = MetamodelValidator.TryNormalizeElementType(raw, out var normalized);

        Assert.True(ok);
        Assert.Equal(expected, normalized);
    }

    [Fact]
    public void IsViewType_DetectsViewAndDiagram()
    {
        Assert.True(MetamodelValidator.IsViewType("View"));
        Assert.True(MetamodelValidator.IsViewType("Diagram"));
        Assert.False(MetamodelValidator.IsViewType("BusinessActor"));
    }
}
