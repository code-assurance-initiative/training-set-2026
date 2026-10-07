using System.Xml;

namespace ReportDesk.Api.Metadata;

/// <summary>
/// Imports the metadata file that accompanies a scanned document (<c>&lt;metadata&gt;&lt;field name="…"&gt;…</c>).
/// Some scanning stations reference shared field definitions from the file's DOCTYPE.
/// </summary>
public sealed class MetadataImporter
{
    public IReadOnlyList<MetadataField> Import(string xml)
    {
        XmlDocument document = new XmlDocument();
        document.XmlResolver = new XmlUrlResolver();
        document.LoadXml(xml);

        var fields = new List<MetadataField>();
        foreach (XmlElement field in document.GetElementsByTagName("field"))
        {
            fields.Add(new MetadataField(field.GetAttribute("name"), field.InnerText.Trim()));
        }

        return fields;
    }
}
