namespace ArchiMateAiStudio.Archimate.Validation;

/// <summary>
/// Normalizes LLM type aliases to ArchiMate 3.2 element types.
/// Ported conceptually from Archi-LLM-plugin ArchiMateSchemaValidator.java.
/// </summary>
public static class MetamodelValidator
{
    private static readonly Dictionary<string, string> Aliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Actor"] = "BusinessActor",
            ["Process"] = "BusinessProcess",
            ["Service"] = "BusinessService",
            ["Application"] = "ApplicationComponent",
            ["ApplicationService"] = "ApplicationService",
            ["TechnologyNode"] = "Node",
            ["TechnologyService"] = "TechnologyService",
            ["UsedBy"] = "ServingRelationship",
            ["Realization"] = "RealizationRelationship",
            ["Assignment"] = "AssignmentRelationship",
            ["Flow"] = "FlowRelationship",
            ["Requirement"] = "Requirement",
            ["Goal"] = "Goal",
            ["Constraint"] = "Constraint",
        };

    public static bool TryNormalizeElementType(string rawType, out string normalizedType)
    {
        if (string.IsNullOrWhiteSpace(rawType))
        {
            normalizedType = string.Empty;
            return false;
        }

        if (Aliases.TryGetValue(rawType.Trim(), out normalizedType!))
        {
            return true;
        }

        normalizedType = rawType.Trim();
        return IsKnownElementType(normalizedType);
    }

    public static bool IsKnownElementType(string type) =>
        type is "BusinessActor" or "BusinessProcess" or "BusinessService" or "ApplicationComponent"
            or "ApplicationService" or "Node" or "TechnologyService" or "Requirement" or "Goal"
            or "Constraint" or "DataObject" or "BusinessObject" or "WorkPackage" or "Plateau" or "Gap";

    public static bool IsViewType(string type) =>
        type.Equals("View", StringComparison.OrdinalIgnoreCase)
        || type.Equals("Diagram", StringComparison.OrdinalIgnoreCase);
}
