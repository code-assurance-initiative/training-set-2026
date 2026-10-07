using System.Xml;
using System.Xml.XPath;

namespace ReportDesk.Api.Metadata;

/// <summary>Reads the fields of one section (<c>&lt;metadata&gt;&lt;{section}&gt;&lt;field …/&gt;</c>).</summary>
public static class MetadataSectionReader
{
    public static IReadOnlyList<MetadataField> ReadSection(string section, string metadataXml)
    {
        XmlConvert.VerifyNCName(section);
        using var reader = XmlReader.Create(new StringReader(metadataXml), SafeXml.ReaderSettings());
        var navigator = new XPathDocument(reader).CreateNavigator();
        var nodes = navigator.Select("/metadata/" + section + "/field[not(@restricted='true')]");
        var fields = new List<MetadataField>();
        while (nodes.MoveNext())
        {
            var node = nodes.Current;
            if (node is not null)
            {
                fields.Add(new MetadataField(node.GetAttribute("name", string.Empty), node.Value));
            }
        }

        return fields;
    }
}
