using ArchiMateAiStudio.Archimate.Ids;
using ArchiMateAiStudio.Archimate.Models;
using ArchiMateAiStudio.Archimate.Parsing;
using ArchiMateAiStudio.Archimate.Patching;
using ArchiMateAiStudio.Archimate.Serialization;
using ArchiMateAiStudio.Domain.Patches;

namespace ArchiMateAiStudio.Archimate.Tests;

public class ModelPatchApplicatorTests
{
    private static string MinimalFixturePath =>
        Path.Combine(AppContext.BaseDirectory, "fixtures", "minimal", "minimal.archimate");

    private static ArchimateDocument LoadMinimal() =>
        ArchiMateXmlParser.ParseFile(MinimalFixturePath);

    [Fact]
    public void Apply_AddsApplicationComponent_ToDocument()
    {
        var document = LoadMinimal();
        var patch = new ModelPatch
        {
            Elements =
            [
                new ElementPatch
                {
                    Type = "ApplicationComponent",
                    Name = "Claims Portal",
                    Id = "id-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                },
            ],
        };

        var result = ModelPatchApplicator.Apply(document, patch);

        Assert.True(result.Success, string.Join("; ", result.Errors));
        Assert.NotNull(result.Document);
        Assert.Equal(2, document.Statistics.ElementCount);

        var added = FindElement(result.Document, "Claims Portal");
        Assert.NotNull(added);
        Assert.Equal("ApplicationComponent", added.Type);
        Assert.Equal("id-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", added.Id);
        Assert.Equal(3, result.Document.Statistics.ElementCount);
    }

    [Fact]
    public void Apply_AddsServingRelationship_ByElementNameRefs()
    {
        var document = LoadMinimal();
        var patch = new ModelPatch
        {
            Elements =
            [
                new ElementPatch
                {
                    Type = "ApplicationComponent",
                    Name = "Claims Portal",
                    Id = "id-bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                },
            ],
            Relationships =
            [
                new RelationshipPatch
                {
                    Type = "ServingRelationship",
                    SourceRef = "Claims Portal",
                    TargetRef = "Quote",
                    Id = "id-cccccccccccccccccccccccccccccccc",
                },
            ],
        };

        var result = ModelPatchApplicator.Apply(document, patch);

        Assert.True(result.Success, string.Join("; ", result.Errors));
        Assert.NotNull(result.Document);

        var relationship = FindRelationship(result.Document!, "id-cccccccccccccccccccccccccccccccc");
        Assert.NotNull(relationship);
        Assert.Equal("ServingRelationship", relationship.Type);
        Assert.Equal("id-bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", relationship.Source);
        Assert.Equal("id-minimal-process-quote000000001", relationship.Target);
        Assert.Equal(2, result.Document.Statistics.RelationshipCount);
    }

    [Fact]
    public void Apply_AssignsValidArchiId_WhenLlmOmitsId()
    {
        var document = LoadMinimal();
        var patch = new ModelPatch
        {
            Elements =
            [
                new ElementPatch
                {
                    Type = "ApplicationComponent",
                    Name = "Billing Engine",
                },
            ],
        };

        var result = ModelPatchApplicator.Apply(document, patch);

        Assert.True(result.Success, string.Join("; ", result.Errors));
        var added = FindElement(result.Document!, "Billing Engine");
        Assert.NotNull(added);
        Assert.True(ArchiMateIdGenerator.IsValid(added.Id), $"Expected Archi id, got '{added.Id}'");
    }

    [Fact]
    public void Apply_RejectsUnknownElementType()
    {
        var document = LoadMinimal();
        var patch = new ModelPatch
        {
            Elements =
            [
                new ElementPatch
                {
                    Type = "NotARealType",
                    Name = "Ghost",
                },
            ],
        };

        var result = ModelPatchApplicator.Apply(document, patch);

        Assert.False(result.Success);
        Assert.Null(result.Document);
        Assert.Contains(result.Errors, e => e.Contains("NotARealType", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(2, document.Statistics.ElementCount);
    }

    [Fact]
    public void Apply_RemovesElement_AndCascadesRelationships()
    {
        var document = LoadMinimal();
        var patch = new ModelPatch
        {
            RemoveElementIds = ["id-minimal-actor-customer000000001"],
        };

        var result = ModelPatchApplicator.Apply(document, patch);

        Assert.True(result.Success, string.Join("; ", result.Errors));
        Assert.NotNull(result.Document);
        Assert.Null(FindElementById(result.Document, "id-minimal-actor-customer000000001"));
        Assert.Equal(0, result.Document.Statistics.RelationshipCount);
        Assert.Equal(1, result.Document.Statistics.ElementCount);
        Assert.Equal(1, document.Statistics.RelationshipCount);
    }

    [Fact]
    public void Apply_RoundTrip_SerializeParse_FindsNewElements()
    {
        var document = LoadMinimal();
        var patch = new ModelPatch
        {
            Elements =
            [
                new ElementPatch
                {
                    Type = "ApplicationComponent",
                    Name = "Portal",
                    Id = "id-dddddddddddddddddddddddddddddddd",
                },
            ],
            Relationships =
            [
                new RelationshipPatch
                {
                    Type = "ServingRelationship",
                    SourceRef = "Portal",
                    TargetRef = "Quote",
                },
            ],
        };

        var result = ModelPatchApplicator.Apply(document, patch);
        Assert.True(result.Success, string.Join("; ", result.Errors));

        var xml = ArchiMateXmlSerializer.Serialize(result.Document!);
        var reparsed = ArchiMateXmlParser.Parse(xml);

        Assert.NotNull(FindElement(reparsed, "Portal"));
        Assert.Equal(3, reparsed.Statistics.ElementCount);
        Assert.Equal(2, reparsed.Statistics.RelationshipCount);
    }

    private static ArchimateConcept? FindElement(ArchimateDocument document, string name) =>
        ArchiMateXmlParser.CollectConcepts(document)
            .FirstOrDefault(c =>
                c.Kind == ArchimateConceptKind.Element
                && string.Equals(c.Name, name, StringComparison.Ordinal));

    private static ArchimateConcept? FindElementById(ArchimateDocument document, string id) =>
        ArchiMateXmlParser.CollectConcepts(document)
            .FirstOrDefault(c => c.Id == id);

    private static ArchimateConcept? FindRelationship(ArchimateDocument document, string id) =>
        ArchiMateXmlParser.CollectConcepts(document)
            .FirstOrDefault(c =>
                c.Kind == ArchimateConceptKind.Relationship && c.Id == id);
}
