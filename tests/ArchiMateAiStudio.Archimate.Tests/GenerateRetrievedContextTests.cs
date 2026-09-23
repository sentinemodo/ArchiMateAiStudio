using ArchiMateAiStudio.Application.Generate;
using ArchiMateAiStudio.Application.Proposals;
using ArchiMateAiStudio.Application.Rag;
using ArchiMateAiStudio.Domain.Ports;
using ArchiMateAiStudio.Domain.Rag;
using ArchiMateAiStudio.Infrastructure.Llm;
using ArchiMateAiStudio.Infrastructure.Persistence;
using ArchiMateAiStudio.Infrastructure.Rag;

namespace ArchiMateAiStudio.Archimate.Tests;

public class GenerateRetrievedContextTests
{
    [Fact]
    public async Task Generate_InjectsRetrievedContext_IntoLlmUserMessage()
    {
        var models = new InMemoryArchimateModelRepository();
        var proposals = new InMemoryChangeProposalRepository();
        var embeddings = new StubEmbeddingClient();
        var rag = new InMemoryRagIndex(embeddings);
        var indexer = new ModelRagIndexer(rag);
        var proposalService = new ChangeProposalService(proposals, models, indexer);
        var recording = new RecordingLlmChatClient();

        var xml = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "fixtures",
            "minimal",
            "minimal.archimate"));
        var model = await models.CreateAsync("Minimal", xml);

        await rag.ReplaceByKindAsync(
            model.Id,
            RagChunkKind.Element,
            [
                new RagChunkInput(
                    RagChunkKind.Element,
                    "id-minimal-actor-customer000000001",
                    "Type: BusinessActor\nName: Customer\nDocumentation: The insured party."),
            ]);

        var service = new GenerateModelChangesService(models, recording, proposalService, rag);
        var result = await service.GenerateAsync(model.Id, "Add ApplicationComponent Claims Portal.");

        Assert.Equal(GenerateModelChangesOutcome.Success, result.Outcome);
        Assert.NotNull(recording.LastRequest);
        var userContent = recording.LastRequest!.Messages.Single(m => m.Role == "user").Content;
        Assert.Contains("Retrieved context", userContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Customer", userContent, StringComparison.Ordinal);
    }

    private sealed class RecordingLlmChatClient : ILlmChatClient
    {
        private readonly StubLlmChatClient _inner = new();

        public LlmRequest? LastRequest { get; private set; }

        public Task<LlmCompletion> CompleteAsync(
            LlmRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return _inner.CompleteAsync(request, cancellationToken);
        }
    }
}
