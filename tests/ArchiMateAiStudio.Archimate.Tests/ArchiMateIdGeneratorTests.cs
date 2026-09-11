using ArchiMateAiStudio.Archimate.Ids;

namespace ArchiMateAiStudio.Archimate.Tests;

public class ArchiMateIdGeneratorTests
{
    [Fact]
    public void NewId_UsesArchiPrefixAnd32HexChars()
    {
        var id = ArchiMateIdGenerator.NewId();

        Assert.StartsWith(ArchiMateIdGenerator.Prefix, id);
        Assert.True(ArchiMateIdGenerator.IsValid(id));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("id-nothex")]
    [InlineData("id-123")]
    public void IsValid_RejectsInvalidIds(string? id)
    {
        Assert.False(ArchiMateIdGenerator.IsValid(id));
    }
}
