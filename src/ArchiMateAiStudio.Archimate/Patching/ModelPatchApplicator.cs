using ArchiMateAiStudio.Archimate.Ids;
using ArchiMateAiStudio.Archimate.Models;
using ArchiMateAiStudio.Archimate.Validation;
using ArchiMateAiStudio.Domain.Patches;

namespace ArchiMateAiStudio.Archimate.Patching;

/// <summary>
/// Applies a <see cref="ModelPatch"/> to an <see cref="ArchimateDocument"/> without mutating the input.
/// Diagram add/remove is deferred for Phase 0 (see comment in <see cref="Apply"/>).
/// </summary>
public static class ModelPatchApplicator
{
    public static ModelPatchApplyResult Apply(ArchimateDocument document, ModelPatch patch)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(patch);

        var errors = new List<string>();
        var workingFolders = CloneFolders(document.Folders);

        // Diagram patches (add / remove views) are intentionally skipped in Phase 0.
        // Element and relationship add/remove cover the MVP model foundation; diagram
        // support can land when view-management is wired (mvp.md §7 later slices).

        var idMap = new Dictionary<string, string>(StringComparer.Ordinal);
        var nameToId = BuildNameIndex(workingFolders);

        foreach (var elementPatch in patch.Elements)
        {
            if (!MetamodelValidator.TryNormalizeElementType(elementPatch.Type, out var normalizedType)
                || MetamodelValidator.IsViewType(normalizedType)
                || MetamodelValidator.IsKnownRelationshipType(normalizedType))
            {
                errors.Add($"Unknown or unsupported element type '{elementPatch.Type}'.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(elementPatch.Name))
            {
                errors.Add("Element name is required.");
                continue;
            }

            var assignedId = ResolveOrGenerateId(elementPatch.Id, idMap);
            var concept = new ArchimateConcept
            {
                Id = assignedId,
                Type = normalizedType,
                Kind = ArchimateConceptKind.Element,
                Name = elementPatch.Name,
                Documentation = elementPatch.Documentation,
                Properties = elementPatch.Properties,
            };

            var folderType = MetamodelValidator.FolderTypeForElement(normalizedType);
            var folder = EnsureFolder(workingFolders, folderType, FolderDisplayName(folderType));
            folder.Elements.Add(concept);

            nameToId[elementPatch.Name] = assignedId;
            if (!string.IsNullOrWhiteSpace(elementPatch.Id)
                && !string.Equals(elementPatch.Id, assignedId, StringComparison.Ordinal))
            {
                idMap[elementPatch.Id] = assignedId;
            }
        }

        foreach (var relationshipPatch in patch.Relationships)
        {
            if (!MetamodelValidator.TryNormalizeRelationshipType(relationshipPatch.Type, out var normalizedType))
            {
                errors.Add($"Unknown or unsupported relationship type '{relationshipPatch.Type}'.");
                continue;
            }

            if (!TryResolveRef(relationshipPatch.SourceRef, nameToId, idMap, workingFolders, out var sourceId))
            {
                errors.Add($"Could not resolve relationship source '{relationshipPatch.SourceRef}'.");
                continue;
            }

            if (!TryResolveRef(relationshipPatch.TargetRef, nameToId, idMap, workingFolders, out var targetId))
            {
                errors.Add($"Could not resolve relationship target '{relationshipPatch.TargetRef}'.");
                continue;
            }

            var assignedId = ResolveOrGenerateId(relationshipPatch.Id, idMap);
            var concept = new ArchimateConcept
            {
                Id = assignedId,
                Type = normalizedType,
                Kind = ArchimateConceptKind.Relationship,
                Name = relationshipPatch.Name,
                Source = sourceId,
                Target = targetId,
            };

            var relations = EnsureFolder(workingFolders, "relations", "Relations");
            relations.Elements.Add(concept);

            if (!string.IsNullOrWhiteSpace(relationshipPatch.Id)
                && !string.Equals(relationshipPatch.Id, assignedId, StringComparison.Ordinal))
            {
                idMap[relationshipPatch.Id] = assignedId;
            }
        }

        if (patch.RemoveElementIds.Count > 0)
        {
            var removeIds = new HashSet<string>(patch.RemoveElementIds, StringComparer.Ordinal);
            RemoveConcepts(workingFolders, removeIds);
        }

        if (errors.Count > 0)
        {
            return ModelPatchApplyResult.Fail(errors);
        }

        var resultDocument = new ArchimateDocument
        {
            Name = document.Name,
            Id = document.Id,
            Version = document.Version,
            Folders = FreezeFolders(workingFolders),
        };

        return ModelPatchApplyResult.Ok(resultDocument);
    }

    private static string ResolveOrGenerateId(string? requestedId, IDictionary<string, string> idMap)
    {
        if (ArchiMateIdGenerator.IsValid(requestedId))
        {
            return requestedId!;
        }

        var generated = ArchiMateIdGenerator.NewId();
        if (!string.IsNullOrWhiteSpace(requestedId))
        {
            idMap[requestedId] = generated;
        }

        return generated;
    }

    private static bool TryResolveRef(
        string reference,
        IReadOnlyDictionary<string, string> nameToId,
        IReadOnlyDictionary<string, string> idMap,
        IReadOnlyList<MutableFolder> folders,
        out string resolvedId)
    {
        resolvedId = string.Empty;
        if (string.IsNullOrWhiteSpace(reference))
        {
            return false;
        }

        if (idMap.TryGetValue(reference, out var mapped))
        {
            resolvedId = mapped;
            return true;
        }

        if (FindConceptById(folders, reference) is not null)
        {
            resolvedId = reference;
            return true;
        }

        if (nameToId.TryGetValue(reference, out var byName))
        {
            resolvedId = byName;
            return true;
        }

        return false;
    }

    private static Dictionary<string, string> BuildNameIndex(IEnumerable<MutableFolder> folders)
    {
        var index = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var concept in EnumerateConcepts(folders))
        {
            if (concept.Kind == ArchimateConceptKind.Element
                && !string.IsNullOrWhiteSpace(concept.Name)
                && !index.ContainsKey(concept.Name))
            {
                index[concept.Name] = concept.Id;
            }
        }

        return index;
    }

    private static void RemoveConcepts(List<MutableFolder> folders, HashSet<string> removeIds)
    {
        foreach (var folder in folders)
        {
            RemoveConcepts(folder.Folders, removeIds);
            folder.Elements.RemoveAll(concept =>
                removeIds.Contains(concept.Id)
                || (concept.Kind == ArchimateConceptKind.Relationship
                    && ((concept.Source is not null && removeIds.Contains(concept.Source))
                        || (concept.Target is not null && removeIds.Contains(concept.Target)))));
        }
    }

    private static MutableFolder EnsureFolder(List<MutableFolder> folders, string type, string name)
    {
        var existing = folders.FirstOrDefault(f =>
            string.Equals(f.Type, type, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return existing;
        }

        var created = new MutableFolder
        {
            Name = name,
            Id = ArchiMateIdGenerator.NewId(),
            Type = type,
        };
        folders.Add(created);
        return created;
    }

    private static string FolderDisplayName(string folderType) =>
        folderType switch
        {
            "business" => "Business",
            "application" => "Application",
            "technology" => "Technology",
            "motivation" => "Motivation",
            "implementation_migration" => "Implementation & Migration",
            "relations" => "Relations",
            _ => "Other",
        };

    private static List<MutableFolder> CloneFolders(IEnumerable<ArchimateFolder> folders) =>
        folders.Select(CloneFolder).ToList();

    private static MutableFolder CloneFolder(ArchimateFolder folder) =>
        new()
        {
            Name = folder.Name,
            Id = folder.Id,
            Type = folder.Type,
            Folders = folder.Folders.Select(CloneFolder).ToList(),
            Elements = folder.Elements.Select(CloneConcept).ToList(),
        };

    private static ArchimateConcept CloneConcept(ArchimateConcept concept) =>
        new()
        {
            Id = concept.Id,
            Type = concept.Type,
            Kind = concept.Kind,
            Name = concept.Name,
            Source = concept.Source,
            Target = concept.Target,
            Viewpoint = concept.Viewpoint,
            Documentation = concept.Documentation,
            ArchimateElementRef = concept.ArchimateElementRef,
            ArchimateRelationshipRef = concept.ArchimateRelationshipRef,
            Bounds = concept.Bounds is null
                ? null
                : new ArchimateBounds
                {
                    X = concept.Bounds.X,
                    Y = concept.Bounds.Y,
                    Width = concept.Bounds.Width,
                    Height = concept.Bounds.Height,
                },
            Properties = concept.Properties.ToDictionary(static p => p.Key, static p => p.Value),
            Attributes = concept.Attributes.ToDictionary(static a => a.Key, static a => a.Value),
            Children = concept.Children.Select(CloneConcept).ToList(),
        };

    private static IReadOnlyList<ArchimateFolder> FreezeFolders(IEnumerable<MutableFolder> folders) =>
        folders.Select(FreezeFolder).ToList();

    private static ArchimateFolder FreezeFolder(MutableFolder folder) =>
        new()
        {
            Name = folder.Name,
            Id = folder.Id,
            Type = folder.Type,
            Folders = folder.Folders.Select(FreezeFolder).ToList(),
            Elements = folder.Elements.ToList(),
        };

    private static IEnumerable<ArchimateConcept> EnumerateConcepts(IEnumerable<MutableFolder> folders)
    {
        foreach (var folder in folders)
        {
            foreach (var element in folder.Elements)
            {
                yield return element;
            }

            foreach (var nested in EnumerateConcepts(folder.Folders))
            {
                yield return nested;
            }
        }
    }

    private static ArchimateConcept? FindConceptById(IEnumerable<MutableFolder> folders, string id) =>
        EnumerateConcepts(folders).FirstOrDefault(c => c.Id == id);

    private sealed class MutableFolder
    {
        public required string Name { get; init; }

        public required string Id { get; init; }

        public string? Type { get; init; }

        public List<MutableFolder> Folders { get; init; } = [];

        public List<ArchimateConcept> Elements { get; init; } = [];
    }
}
