namespace ArchiMateAiStudio.Infrastructure.Persistence.Ef;

public sealed class ModelEntity
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string ArchimateXml { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
