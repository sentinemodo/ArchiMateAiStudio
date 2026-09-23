namespace ArchiMateAiStudio.Domain.Models;

/// <summary>
/// Persisted ArchiMate model metadata with canonical <c>.archimate</c> XML payload.
/// </summary>
public sealed class ModelRecord
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    /// <summary>Canonical Archi-compatible XML (source of truth).</summary>
    public required string ArchimateXml { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
