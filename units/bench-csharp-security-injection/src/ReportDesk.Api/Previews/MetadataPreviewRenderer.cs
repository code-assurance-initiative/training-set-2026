using System.Globalization;
using System.Text;
using System.Xml;

namespace ReportDesk.Api.Previews;

/// <summary>Renders a document's metadata as an HTML definition list for the archive's preview pane.</summary>
public static class MetadataPreviewRenderer
{
    public static string Render(XmlDocument metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        var html = new StringBuilder("<dl class=\"metadata\">");
        foreach (XmlElement field in metadata.GetElementsByTagName("field"))
        {
            if (field.GetAttribute("restricted") == "true")
            {
                continue;
            }

            var label = field.GetAttribute("label");
            var value = field.InnerText;
            html.Append(CultureInfo.InvariantCulture, $"<dt>{label}</dt><dd title=\"{value}\">{value}</dd>");
        }

        return html.Append("</dl>").ToString();
    }
}
