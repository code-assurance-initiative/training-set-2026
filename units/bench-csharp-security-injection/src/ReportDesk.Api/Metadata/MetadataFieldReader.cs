using System.Xml;
using System.Xml.XPath;

namespace ReportDesk.Api.Metadata;

/// <summary>Reads one named field from a document's metadata; restricted fields are never returned.</summary>
public static class MetadataFieldReader
{
    public static IReadOnlyList<string> ReadField(string fieldName, string metadataXml)
    {
        using var reader = XmlReader.Create(new StringReader(metadataXml), SafeXml.ReaderSettings());
        var navigator = new XPathDocument(reader).CreateNavigator();
        var nodes = navigator.Select("/metadata/field[@name='" + fieldName + "' and not(@restricted='true')]");
        var values = new List<string>();
        while (nodes.MoveNext())
        {
            values.Add(nodes.Current?.Value ?? string.Empty);
        }

        return values;
    }
}
