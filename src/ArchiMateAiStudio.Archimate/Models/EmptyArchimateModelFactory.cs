using ArchiMateAiStudio.Archimate.Ids;
using ArchiMateAiStudio.Archimate.Models;
using ArchiMateAiStudio.Archimate.Serialization;

namespace ArchiMateAiStudio.Archimate.Models;

/// <summary>
/// Builds a minimal empty Archi-compatible model with standard top-level folders.
/// </summary>
public static class EmptyArchimateModelFactory
{
    public static ArchimateDocument Create(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new ArchimateDocument
        {
            Name = name.Trim(),
            Id = ArchiMateIdGenerator.NewId(),
            Version = "5.0.0",
            Folders =
            [
                Folder("Business", "business"),
                Folder("Application", "application"),
                Folder("Technology", "technology"),
                Folder("Motivation", "motivation"),
                Folder("Implementation & Migration", "implementation_migration"),
                Folder("Relations", "relations"),
                Folder("Views", "diagrams"),
            ],
        };
    }

    public static string CreateXml(string name) =>
        ArchiMateXmlSerializer.Serialize(Create(name));

    private static ArchimateFolder Folder(string name, string type) =>
        new()
        {
            Name = name,
            Id = ArchiMateIdGenerator.NewId(),
            Type = type,
            Folders = [],
            Elements = [],
        };
}
