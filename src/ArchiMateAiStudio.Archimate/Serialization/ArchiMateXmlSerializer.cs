using System.Text;
using System.Xml.Linq;
using ArchiMateAiStudio.Archimate.Models;
using ArchiMateAiStudio.Archimate.Parsing;

namespace ArchiMateAiStudio.Archimate.Serialization;

public static class ArchiMateXmlSerializer
{
    private static readonly XNamespace Archi = ArchiMateXmlNamespaces.Archi;
    private static readonly XNamespace Xsi = ArchiMateXmlNamespaces.Xsi;

    public static string Serialize(ArchimateDocument document)
    {
        var root = new XElement(
            Archi + "model",
            new XAttribute(XNamespace.Xmlns + "xsi", Xsi),
            new XAttribute(XNamespace.Xmlns + "archimate", Archi),
            new XAttribute("name", document.Name),
            new XAttribute("id", document.Id),
            document.Version is not null ? new XAttribute("version", document.Version) : null,
            document.Folders.Select(SerializeFolder));

        var declaration = new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
        var builder = new StringBuilder();
        using var writer = new StringWriter(builder);
        declaration.Save(writer, SaveOptions.DisableFormatting);
        return builder.ToString();
    }

    private static XElement SerializeFolder(ArchimateFolder folder) =>
        new(
            Archi + "folder",
            new XAttribute("name", folder.Name),
            new XAttribute("id", folder.Id),
            folder.Type is not null ? new XAttribute("type", folder.Type) : null,
            folder.Folders.Select(SerializeFolder),
            folder.Elements.Select(SerializeConcept));

    private static XElement SerializeConcept(ArchimateConcept concept)
    {
        var element = new XElement(Archi + "element");
        element.SetAttributeValue(Xsi + "type", $"archimate:{concept.Type}");
        element.SetAttributeValue("id", concept.Id);

        if (concept.Name is not null)
        {
            element.SetAttributeValue("name", concept.Name);
        }

        if (concept.Source is not null)
        {
            element.SetAttributeValue("source", concept.Source);
        }

        if (concept.Target is not null)
        {
            element.SetAttributeValue("target", concept.Target);
        }

        if (concept.Viewpoint is not null)
        {
            element.SetAttributeValue("viewpoint", concept.Viewpoint);
        }

        if (concept.ArchimateElementRef is not null)
        {
            element.SetAttributeValue("archimateElement", concept.ArchimateElementRef);
        }

        if (concept.ArchimateRelationshipRef is not null)
        {
            element.SetAttributeValue("archimateRelationship", concept.ArchimateRelationshipRef);
        }

        foreach (var (key, value) in concept.Attributes)
        {
            element.SetAttributeValue(key, value);
        }

        if (concept.Documentation is not null)
        {
            element.Add(new XElement(Archi + "documentation", concept.Documentation));
        }

        foreach (var (key, value) in concept.Properties)
        {
            element.Add(new XElement(Archi + "property", new XAttribute("key", key), new XAttribute("value", value)));
        }

        if (concept.Bounds is not null)
        {
            element.Add(
                new XElement(
                    Archi + "bounds",
                    new XAttribute("x", concept.Bounds.X),
                    new XAttribute("y", concept.Bounds.Y),
                    new XAttribute("width", concept.Bounds.Width),
                    new XAttribute("height", concept.Bounds.Height)));
        }

        foreach (var child in concept.Children)
        {
            element.Add(SerializeConcept(child));
        }

        return element;
    }
}
