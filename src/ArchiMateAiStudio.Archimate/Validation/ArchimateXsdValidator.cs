using System.Reflection;
using System.Xml;
using System.Xml.Schema;

namespace ArchiMateAiStudio.Archimate.Validation;

/// <summary>
/// Validates Open Group ArchiMate exchange XML against bundled XSD schemas.
/// </summary>
public static class ArchimateXsdValidator
{
    private static readonly Lazy<XmlSchemaSet> SchemaSet = new(CreateSchemaSet);

    public static XsdValidationResult Validate(string xml)
    {
        var errors = new List<string>();

        try
        {
            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = SchemaSet.Value,
                DtdProcessing = DtdProcessing.Prohibit,
            };

            settings.ValidationEventHandler += (_, args) =>
            {
                errors.Add($"{args.Severity}: {args.Message}");
            };

            using var reader = XmlReader.Create(new StringReader(xml), settings);
            while (reader.Read())
            {
            }
        }
        catch (XmlSchemaException ex)
        {
            errors.Add($"Schema load error: {ex.Message}");
        }
        catch (XmlException ex)
        {
            errors.Add($"XML parse error: {ex.Message}");
        }

        return new XsdValidationResult { Errors = errors };
    }

    private static XmlSchemaSet CreateSchemaSet()
    {
        var schemas = new XmlSchemaSet
        {
            XmlResolver = new XmlUrlResolver(),
        };

        var schemaPath = ResolveSchemaPath("archimate3_Model.xsd");
        schemas.Add(null, schemaPath);
        schemas.Compile();
        return schemas;
    }

    private static string ResolveSchemaPath(string fileName)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Schemas", fileName),
            Path.Combine(GetAssemblyDirectory(), "Schemas", fileName),
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException(
            $"ArchiMate XSD schema '{fileName}' not found. Expected under Schemas/ next to the assembly.");
    }

    private static string GetAssemblyDirectory() =>
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        ?? AppContext.BaseDirectory;
}
