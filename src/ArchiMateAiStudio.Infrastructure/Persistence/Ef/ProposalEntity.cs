namespace ArchiMateAiStudio.Infrastructure.Persistence.Ef;

public sealed class ProposalEntity
{
    public Guid Id { get; set; }

    public Guid ModelId { get; set; }

    public required string PatchJson { get; set; }

    public required string Source { get; set; }

    public required string Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
