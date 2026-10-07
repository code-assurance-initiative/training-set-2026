using System.Globalization;
using System.Text.Encodings.Web;
using System.Xml;

namespace ReportDesk.Api.Previews;

/// <summary>Renders the summary card the archive shows in result lists, from a document's metadata.</summary>
public sealed class DocumentCardRenderer(HtmlEncoder encoder)
{
    private readonly HtmlEncoder _encoder = encoder;

    public string Render(XmlDocument metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        var title = metadata.SelectSingleNode("/metadata/title")?.InnerText ?? "Untitled";
        var header = $"<header class=\"card-title\" title=\"{_encoder.Encode(title)}\">{_encoder.Encode(title)}</header>";
        return $"<article class=\"card\">{header}{RenderFooter(metadata)}</article>";
    }

    /// <summary>
    /// The page-count footer. Scanned documents whose metadata carries no usable page count get no footer at all, so
    /// the card never states a length it does not know.
    /// </summary>
    private static string RenderFooter(XmlDocument metadata)
    {
        var text = metadata.SelectSingleNode("/metadata/pages")?.InnerText;
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var pages))
        {
            return string.Empty;
        }

        return $"<footer class=\"card-pages\" data-pages=\"{pages}\">{pages} pages</footer>";
    }
}
