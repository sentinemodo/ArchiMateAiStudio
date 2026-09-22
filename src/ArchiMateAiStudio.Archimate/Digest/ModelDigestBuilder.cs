using System.Text;
using ArchiMateAiStudio.Archimate.Models;

namespace ArchiMateAiStudio.Archimate.Digest;

/// <summary>
/// Builds a compact human-readable model digest from <see cref="DocumentStatistics"/>.
/// </summary>
public static class ModelDigestBuilder
{
    public static string Build(ArchimateDocument document)
    {
        var stats = document.Statistics;
        var builder = new StringBuilder();
        builder.AppendLine("MODEL DIGEST");
        builder.AppendLine($"Name: {document.Name}");
        builder.AppendLine($"Elements: {stats.ElementCount}");
        builder.AppendLine($"Relationships: {stats.RelationshipCount}");
        builder.AppendLine($"Diagrams/views: {stats.DiagramCount}");
        builder.AppendLine($"Folders: {stats.FolderCount}");
        return builder.ToString().TrimEnd();
    }
}
