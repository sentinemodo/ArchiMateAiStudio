namespace ArchiMateAiStudio.Archimate.Validation;

public sealed class XsdValidationResult
{
    public bool IsValid => Errors.Count == 0;

    public IReadOnlyList<string> Errors { get; init; } = [];
}
