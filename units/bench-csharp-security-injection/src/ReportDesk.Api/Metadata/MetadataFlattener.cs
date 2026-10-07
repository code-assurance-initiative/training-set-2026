using System.Xml.Linq;

namespace ReportDesk.Api.Metadata;

/// <summary>
/// Flattens structured metadata (nested elements) into path/value pairs, e.g.
/// <c>metadata/case/parties/claimant</c> = "…", for the search index.
/// </summary>
public static class MetadataFlattener
{
    public static IReadOnlyList<MetadataField> Flatten(XElement root)
    {
        ArgumentNullException.ThrowIfNull(root);
        var fields = new List<MetadataField>();
        Collect(root, root.Name.LocalName, fields);
        return fields;
    }

    private static void Collect(XElement element, string path, List<MetadataField> fields)
    {
        if (!element.HasElements)
        {
            fields.Add(new MetadataField(path, element.Value.Trim()));
            return;
        }

        foreach (var child in element.Elements())
        {
            var childPath = $"{path}/{child.Name.LocalName}";
            Collect(child, childPath, fields);
        }
    }
}
