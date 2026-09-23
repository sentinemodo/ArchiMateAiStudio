using ArchiMateAiStudio.Domain.Ports;
using ArchiMateAiStudio.Infrastructure.Llm;

namespace ArchiMateAiStudio.Archimate.Tests;

public class StubLlmChatClientTests
{
    [Fact]
    public async Task CompleteAsync_WhenTextMentionsClaims_ReturnsClaimsPortalPatch()
    {
        var client = new StubLlmChatClient();
        var completion = await client.CompleteAsync(
            new LlmRequest("sys", [new LlmMessage("user", "EXTRACTED TEXT:\nClaims handling document")]));

        Assert.Contains("Claims Portal", completion.Content);
    }

    [Fact]
    public async Task CompleteAsync_WhenNoKeywords_ReturnsApplicationComponentFromFirstLine()
    {
        var client = new StubLlmChatClient();
        var completion = await client.CompleteAsync(
            new LlmRequest(
                "sys",
                [new LlmMessage("user", "EXTRACTED TEXT:\nBilling Engine Service\nMore detail")]));

        Assert.Contains("Billing Engine Service", completion.Content);
        Assert.Contains("ApplicationComponent", completion.Content);
    }

    [Fact]
    public async Task CompleteAsync_WhenNoMeaningfulText_ReturnsImportedCapability()
    {
        var client = new StubLlmChatClient();
        var completion = await client.CompleteAsync(
            new LlmRequest("sys", [new LlmMessage("user", "please map this")]));

        Assert.Contains("Imported Capability", completion.Content);
        Assert.Contains("ApplicationComponent", completion.Content);
    }
}
