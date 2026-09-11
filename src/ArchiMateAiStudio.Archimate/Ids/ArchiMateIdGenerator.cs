namespace ArchiMateAiStudio.Archimate.Ids;

/// <summary>
/// Generates Archi-compatible element IDs: id- + 32 lowercase hex characters.
/// </summary>
public static class ArchiMateIdGenerator
{
    public const string Prefix = "id-";

    public static string NewId()
    {
        return Prefix + Guid.NewGuid().ToString("N");
    }

    public static bool IsValid(string? id)
    {
        if (string.IsNullOrWhiteSpace(id) || !id.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var hex = id[Prefix.Length..];
        return hex.Length == 32 && hex.All(static c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
    }
}
