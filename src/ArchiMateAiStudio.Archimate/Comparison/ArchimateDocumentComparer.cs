using ArchiMateAiStudio.Archimate.Models;
using ArchiMateAiStudio.Archimate.Parsing;

namespace ArchiMateAiStudio.Archimate.Comparison;

public static class ArchimateDocumentComparer
{
    public static DocumentComparisonResult Compare(ArchimateDocument left, ArchimateDocument right)
    {
        var differences = new List<string>();

        if (!string.Equals(left.Name, right.Name, StringComparison.Ordinal))
        {
            differences.Add($"Model name mismatch: '{left.Name}' vs '{right.Name}'.");
        }

        if (!string.Equals(left.Id, right.Id, StringComparison.Ordinal))
        {
            differences.Add($"Model id mismatch: '{left.Id}' vs '{right.Id}'.");
        }

        CompareStatistics(left.Statistics, right.Statistics, differences);
        CompareConceptSets(left, right, differences);

        return new DocumentComparisonResult
        {
            IsEquivalent = differences.Count == 0,
            Differences = differences,
        };
    }

    private static void CompareStatistics(
        DocumentStatistics left,
        DocumentStatistics right,
        List<string> differences)
    {
        if (left.ElementCount != right.ElementCount)
        {
            differences.Add($"Element count mismatch: {left.ElementCount} vs {right.ElementCount}.");
        }

        if (left.RelationshipCount != right.RelationshipCount)
        {
            differences.Add(
                $"Relationship count mismatch: {left.RelationshipCount} vs {right.RelationshipCount}.");
        }

        if (left.DiagramCount != right.DiagramCount)
        {
            differences.Add($"Diagram count mismatch: {left.DiagramCount} vs {right.DiagramCount}.");
        }
    }

    private static void CompareConceptSets(
        ArchimateDocument left,
        ArchimateDocument right,
        List<string> differences)
    {
        var leftConcepts = ArchiMateXmlParser
            .CollectConcepts(left)
            .Where(c => c.Kind is ArchimateConceptKind.Element or ArchimateConceptKind.Relationship)
            .ToDictionary(c => c.Id, StringComparer.Ordinal);

        var rightConcepts = ArchiMateXmlParser
            .CollectConcepts(right)
            .Where(c => c.Kind is ArchimateConceptKind.Element or ArchimateConceptKind.Relationship)
            .ToDictionary(c => c.Id, StringComparer.Ordinal);

        foreach (var (id, concept) in leftConcepts)
        {
            if (!rightConcepts.TryGetValue(id, out var other))
            {
                differences.Add($"Missing concept in round-trip output: {id}.");
                continue;
            }

            if (!string.Equals(concept.Type, other.Type, StringComparison.Ordinal))
            {
                differences.Add($"Type mismatch for {id}: {concept.Type} vs {other.Type}.");
            }

            if (!string.Equals(concept.Name, other.Name, StringComparison.Ordinal))
            {
                differences.Add($"Name mismatch for {id}: '{concept.Name}' vs '{other.Name}'.");
            }

            if (concept.Kind == ArchimateConceptKind.Relationship)
            {
                if (!string.Equals(concept.Source, other.Source, StringComparison.Ordinal)
                    || !string.Equals(concept.Target, other.Target, StringComparison.Ordinal))
                {
                    differences.Add($"Relationship endpoints mismatch for {id}.");
                }
            }
        }

        foreach (var id in rightConcepts.Keys)
        {
            if (!leftConcepts.ContainsKey(id))
            {
                differences.Add($"Unexpected concept in round-trip output: {id}.");
            }
        }
    }
}

public sealed class DocumentComparisonResult
{
    public required bool IsEquivalent { get; init; }

    public IReadOnlyList<string> Differences { get; init; } = [];
}
