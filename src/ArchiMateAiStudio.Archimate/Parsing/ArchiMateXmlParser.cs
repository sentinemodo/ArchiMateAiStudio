using System.Xml.Linq;
using ArchiMateAiStudio.Archimate.Models;

namespace ArchiMateAiStudio.Archimate.Parsing;

public static class ArchiMateXmlParser
{
    private static readonly XNamespace Archi = ArchiMateXmlNamespaces.Archi;
    private static readonly XNamespace Xsi = ArchiMateXmlNamespaces.Xsi;

    private static readonly HashSet<string> ReservedAttributes =
    [
        "id",
        "name",
        "source",
        "target",
        "viewpoint",
        "type",
        "archimateElement",
        "archimateRelationship",
    ];

    public static ArchimateDocument Parse(string xml)
    {
        var document = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        var root = document.Root ?? throw new InvalidOperationException("Missing root element.");

        if (root.Name.LocalName != "model")
        {
            throw new InvalidOperationException(
                $"Expected archimate:model root element, found '{root.Name.LocalName}'.");
        }

        return new ArchimateDocument
        {
            Name = (string?)root.Attribute("name") ?? string.Empty,
            Id = (string?)root.Attribute("id") ?? string.Empty,
            Version = (string?)root.Attribute("version"),
            Folders = ChildElements(root, "folder").Select(ParseFolder).ToList(),
        };
    }

    public static ArchimateDocument ParseFile(string path)
    {
        var xml = File.ReadAllText(path);
        return Parse(xml);
    }

    public static IReadOnlyList<ArchimateConcept> CollectConcepts(ArchimateDocument document)
    {
        var concepts = new List<ArchimateConcept>();

        void WalkFolders(IEnumerable<ArchimateFolder> folders)
        {
            foreach (var folder in folders)
            {
                concepts.AddRange(folder.Elements);
                WalkFolders(folder.Folders);
            }
        }

        WalkFolders(document.Folders);
        return concepts;
    }

    private static ArchimateFolder ParseFolder(XElement element) =>
        new()
        {
            Name = (string?)element.Attribute("name") ?? string.Empty,
            Id = (string?)element.Attribute("id") ?? string.Empty,
            Type = (string?)element.Attribute("type"),
            Folders = ChildElements(element, "folder").Select(ParseFolder).ToList(),
            Elements = ChildElements(element, "element").Select(ParseConcept).ToList(),
        };

    /// <summary>
    /// Archi .archimate files use unprefixed child tags (folder, element) outside the default namespace.
    /// </summary>
    private static IEnumerable<XElement> ChildElements(XElement parent, string localName) =>
        parent.Elements().Where(element => element.Name.LocalName == localName);

    private static ArchimateConcept ParseConcept(XElement element)
    {
        var xsiType = (string?)element.Attribute(Xsi + "type") ?? string.Empty;
        var type = ExtractTypeName(xsiType);
        var kind = ClassifyKind(type, element);

        return new ArchimateConcept
        {
            Id = (string?)element.Attribute("id") ?? string.Empty,
            Type = type,
            Kind = kind,
            Name = (string?)element.Attribute("name"),
            Source = (string?)element.Attribute("source"),
            Target = (string?)element.Attribute("target"),
            Viewpoint = (string?)element.Attribute("viewpoint"),
            Documentation = ChildElement(element, "documentation")?.Value,
            ArchimateElementRef = (string?)element.Attribute("archimateElement"),
            ArchimateRelationshipRef = (string?)element.Attribute("archimateRelationship"),
            Bounds = ParseBounds(ChildElement(element, "bounds")),
            Properties = ParseProperties(element),
            Attributes = ParseExtraAttributes(element),
            Children = ChildElements(element, "element").Select(ParseConcept).ToList(),
        };
    }

    private static XElement? ChildElement(XElement parent, string localName) =>
        parent.Elements().FirstOrDefault(element => element.Name.LocalName == localName);

    private static ArchimateConceptKind ClassifyKind(string type, XElement element)
    {
        if (type.EndsWith("Relationship", StringComparison.Ordinal)
            || (element.Attribute("source") is not null && element.Attribute("target") is not null))
        {
            return ArchimateConceptKind.Relationship;
        }

        if (type is "ArchimateDiagramModel" or "DiagramModelReference")
        {
            return ArchimateConceptKind.Diagram;
        }

        if (type is "DiagramObject" or "Connection")
        {
            return ArchimateConceptKind.DiagramDecoration;
        }

        if (type is "Note" or "Group" or "DiagramReference")
        {
            return ArchimateConceptKind.DiagramDecoration;
        }

        return ArchimateConceptKind.Element;
    }

    private static string ExtractTypeName(string xsiType)
    {
        if (string.IsNullOrWhiteSpace(xsiType))
        {
            return "Unknown";
        }

        var separator = xsiType.IndexOf(':');
        return separator >= 0 ? xsiType[(separator + 1)..] : xsiType;
    }

    private static ArchimateBounds? ParseBounds(XElement? boundsElement)
    {
        if (boundsElement is null)
        {
            return null;
        }

        return new ArchimateBounds
        {
            X = ParseInt(boundsElement.Attribute("x")),
            Y = ParseInt(boundsElement.Attribute("y")),
            Width = ParseInt(boundsElement.Attribute("width")),
            Height = ParseInt(boundsElement.Attribute("height")),
        };
    }

    private static int ParseInt(XAttribute? attribute) =>
        attribute is not null && int.TryParse((string)attribute, out var value) ? value : 0;

    private static IReadOnlyDictionary<string, string> ParseProperties(XElement element)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var property in ChildElements(element, "property"))
        {
            var key = (string?)property.Attribute("key");
            var value = (string?)property.Attribute("value");

            if (!string.IsNullOrWhiteSpace(key) && value is not null)
            {
                properties[key] = value;
            }
        }

        return properties;
    }

    private static IReadOnlyDictionary<string, string> ParseExtraAttributes(XElement element)
    {
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var attribute in element.Attributes())
        {
            if (attribute.IsNamespaceDeclaration || attribute.Name.Namespace == Xsi)
            {
                continue;
            }

            if (ReservedAttributes.Contains(attribute.Name.LocalName))
            {
                continue;
            }

            attributes[attribute.Name.LocalName] = (string)attribute;
        }

        return attributes;
    }
}
