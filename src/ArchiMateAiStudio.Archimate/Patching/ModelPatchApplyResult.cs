using ArchiMateAiStudio.Archimate.Models;

namespace ArchiMateAiStudio.Archimate.Patching;

/// <summary>
/// Outcome of applying a <see cref="Domain.Patches.ModelPatch"/> to an Archimate document.
/// Validation failures are returned as errors; the applicator does not throw for those cases.
/// </summary>
public sealed class ModelPatchApplyResult
{
    public required bool Success { get; init; }

    /// <summary>
    /// New document when <see cref="Success"/> is true; null on failure.
    /// </summary>
    public ArchimateDocument? Document { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];

    public static ModelPatchApplyResult Ok(ArchimateDocument document) =>
        new() { Success = true, Document = document, Errors = [] };

    public static ModelPatchApplyResult Fail(params string[] errors) =>
        new() { Success = false, Document = null, Errors = errors };

    public static ModelPatchApplyResult Fail(IReadOnlyList<string> errors) =>
        new() { Success = false, Document = null, Errors = errors };
}
