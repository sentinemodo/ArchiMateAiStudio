using ArchiMateAiStudio.Domain.Ports;
using ArchiMateAiStudio.Domain.Rag;
using ArchiMateAiStudio.Infrastructure.Llm;
using ArchiMateAiStudio.Infrastructure.Rag;

namespace ArchiMateAiStudio.Archimate.Tests;

public class RagIndexTests
{
    [Fact]
    public async Task Upsert_ThenSearch_FindsElementByName()
    {
        IEmbeddingClient embeddings = new StubEmbeddingClient();
        IRagIndex index = new InMemoryRagIndex(embeddings);
        var modelId = Guid.NewGuid();

        await index.ReplaceByKindAsync(
            modelId,
            RagChunkKind.Element,
            [
                new RagChunkInput(
                    RagChunkKind.Element,
                    SourceId: "id-actor-customer",
                    Text: "Type: BusinessActor\nName: Customer\nDocumentation: The insured party.",
                    Metadata: new Dictionary<string, string> { ["elementType"] = "BusinessActor" }),
                new RagChunkInput(
                    RagChunkKind.Element,
                    SourceId: "id-process-quote",
                    Text: "Type: BusinessProcess\nName: Quote\nDocumentation: Produce a premium quote.",
                    Metadata: new Dictionary<string, string> { ["elementType"] = "BusinessProcess" }),
            ]);

        var hits = await index.SearchAsync(modelId, "Customer insured", topK: 5);

        Assert.NotEmpty(hits);
        Assert.Contains(hits, h => h.Text.Contains("Customer", StringComparison.Ordinal));
        Assert.Equal(RagChunkKind.Element, hits[0].Kind);
        Assert.Equal(modelId, hits[0].ModelId);
    }

    [Fact]
    public async Task Search_DoesNotReturnChunksFromOtherModels()
    {
        IEmbeddingClient embeddings = new StubEmbeddingClient();
        IRagIndex index = new InMemoryRagIndex(embeddings);
        var modelA = Guid.NewGuid();
        var modelB = Guid.NewGuid();

        await index.ReplaceByKindAsync(
            modelA,
            RagChunkKind.Element,
            [new RagChunkInput(RagChunkKind.Element, "a1", "Type: ApplicationComponent\nName: Claims Portal")]);

        await index.ReplaceByKindAsync(
            modelB,
            RagChunkKind.Element,
            [new RagChunkInput(RagChunkKind.Element, "b1", "Type: ApplicationComponent\nName: Billing Engine")]);

        var hits = await index.SearchAsync(modelA, "Claims Portal", topK: 5);

        Assert.All(hits, h => Assert.Equal(modelA, h.ModelId));
        Assert.DoesNotContain(hits, h => h.Text.Contains("Billing Engine", StringComparison.Ordinal));
    }
}
