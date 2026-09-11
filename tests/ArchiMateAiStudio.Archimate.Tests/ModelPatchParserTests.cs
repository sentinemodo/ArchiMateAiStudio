using ArchiMateAiStudio.Archimate.Parsing;

namespace ArchiMateAiStudio.Archimate.Tests;

public class ModelPatchParserTests
{
    [Fact]
    public void Parse_ExtractsJsonFromProseWrappedResponse()
    {
        const string response = """
            Here are the changes:
            {
              "outputSchemaVersion": "1.0",
              "elements": [
                { "type": "BusinessActor", "name": "Customer" }
              ],
              "relationships": [],
              "diagrams": []
            }
            Done.
            """;

        var patch = ModelPatchParser.Parse(response);

        Assert.Single(patch.Elements);
        Assert.Equal("BusinessActor", patch.Elements[0].Type);
        Assert.Equal("Customer", patch.Elements[0].Name);
    }
}
