using System.Text;
using System.Xml;
using ArchiMateAiStudio.Archimate.Models;
using ArchiMateAiStudio.Archimate.Parsing;

namespace ArchiMateAiStudio.Archimate.Serialization;

/// <summary>
/// Exports an Archi-compatible document to the Open Group ArchiMate 3.x exchange format for XSD validation.
/// </summary>
public static class OpenGroupExchangeSerializer
{
    private const string OpenGroup = ArchiMateXmlNamespaces.OpenGroup;
    private const string Xsi = ArchiMateXmlNamespaces.Xsi;

    private static readonly HashSet<string> SkippedExchangeElementTypes =
        new(StringComparer.Ordinal)
        {
            "Junction",
            "Grouping",
            "Note",
            "DiagramModelReference",
            "ArchimateDiagramModel",
        };

    public static string Serialize(ArchimateDocument document)
    {
        var concepts = ArchiMateXmlParser.CollectConcepts(document);
        var elements = concepts
            .Where(c => c.Kind == ArchimateConceptKind.Element && !SkippedExchangeElementTypes.Contains(c.Type))
            .ToList();
        var exportableElementIds = elements.Select(element => element.Id).ToHashSet(StringComparer.Ordinal);
        var relationships = concepts
            .Where(c => c.Kind == ArchimateConceptKind.Relationship)
            .Where(
                relationship =>
                    relationship.Source is not null
                    && relationship.Target is not null
                    && exportableElementIds.Contains(relationship.Source)
                    && exportableElementIds.Contains(relationship.Target))
            .ToList();
        var idMap = BuildIdentifierMap(document, concepts);

        var builder = new StringBuilder();
        var settings = new XmlWriterSettings
        {
            OmitXmlDeclaration = false,
            Encoding = Encoding.UTF8,
            Indent = false,
        };

        using (var writer = XmlWriter.Create(builder, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("model", OpenGroup);
            writer.WriteAttributeString("xmlns", "xsi", null, Xsi);
            writer.WriteAttributeString("identifier", idMap[document.Id]);
            writer.WriteAttributeString("version", document.Version ?? "3.2");

            writer.WriteStartElement("name", OpenGroup);
            writer.WriteString(document.Name);
            writer.WriteEndElement();

            if (elements.Count > 0)
            {
                writer.WriteStartElement("elements", OpenGroup);
                foreach (var element in elements)
                {
                    WriteElement(writer, element, idMap);
                }

                writer.WriteEndElement();
            }

            if (relationships.Count > 0)
            {
                writer.WriteStartElement("relationships", OpenGroup);
                foreach (var relationship in relationships)
                {
                    WriteRelationship(writer, relationship, idMap);
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return builder.ToString();
    }

    private static void WriteElement(
        XmlWriter writer,
        ArchimateConcept element,
        IReadOnlyDictionary<string, string> idMap)
    {
        writer.WriteStartElement("element", OpenGroup);
        writer.WriteAttributeString("xsi", "type", Xsi, element.Type);
        writer.WriteAttributeString("identifier", idMap[element.Id]);

        writer.WriteStartElement("name", OpenGroup);
        writer.WriteString(element.Name ?? element.Id);
        writer.WriteEndElement();

        if (element.Documentation is not null)
        {
            writer.WriteStartElement("documentation", OpenGroup);
            writer.WriteString(element.Documentation);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteRelationship(
        XmlWriter writer,
        ArchimateConcept relationship,
        IReadOnlyDictionary<string, string> idMap)
    {
        writer.WriteStartElement("relationship", OpenGroup);
        writer.WriteAttributeString("xsi", "type", Xsi, ToExchangeRelationshipType(relationship.Type));
        writer.WriteAttributeString("identifier", idMap[relationship.Id]);
        writer.WriteAttributeString("source", idMap[relationship.Source!]);
        writer.WriteAttributeString("target", idMap[relationship.Target!]);

        if (relationship.Name is not null)
        {
            writer.WriteStartElement("name", OpenGroup);
            writer.WriteString(relationship.Name);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static Dictionary<string, string> BuildIdentifierMap(
        ArchimateDocument document,
        IReadOnlyList<ArchimateConcept> concepts)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [document.Id] = ToExchangeIdentifier(document.Id),
        };

        foreach (var concept in concepts)
        {
            map.TryAdd(concept.Id, ToExchangeIdentifier(concept.Id));
        }

        return map;
    }

    /// <summary>
    /// Maps Archi relationship type names (e.g. AssignmentRelationship) to Open Group exchange names (e.g. Assignment).
    /// </summary>
    public static string ToExchangeRelationshipType(string archiType)
    {
        const string suffix = "Relationship";
        return archiType.EndsWith(suffix, StringComparison.Ordinal)
            ? archiType[..^suffix.Length]
            : archiType;
    }

    public static string ToExchangeIdentifier(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return "id_generated";
        }

        var normalized = id.Replace('-', '_');

        if (char.IsDigit(normalized[0]))
        {
            normalized = "id_" + normalized;
        }

        return normalized;
    }
}
