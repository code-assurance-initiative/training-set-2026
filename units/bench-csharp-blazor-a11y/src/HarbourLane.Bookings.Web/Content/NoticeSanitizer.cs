using Ganss.Xss;

namespace HarbourLane.Bookings.Web.Content;

/// <summary>Reduces staff-written notice HTML to a small allow-list: text formatting and links, nothing executable.</summary>
public sealed class NoticeSanitizer
{
    private readonly HtmlSanitizer _sanitizer;

    public NoticeSanitizer()
    {
        _sanitizer = new HtmlSanitizer(new HtmlSanitizerOptions
        {
            AllowedTags = new HashSet<string>(["p", "strong", "em", "a", "br", "ul", "li"], StringComparer.OrdinalIgnoreCase),
            AllowedAttributes = new HashSet<string>(["href", "title"], StringComparer.OrdinalIgnoreCase),
            AllowedSchemes = new HashSet<string>(["https", "mailto"], StringComparer.OrdinalIgnoreCase),
            UriAttributes = new HashSet<string>(["href"], StringComparer.OrdinalIgnoreCase),
        });
    }

    public string Sanitize(string html) => _sanitizer.Sanitize(html ?? string.Empty);
}
