namespace HarbourLane.Bookings.Content;

/// <summary>The banner shown at the top of every public page, written by staff (HTML).</summary>
public sealed record SiteNotice(string Html, DateTimeOffset UpdatedAt)
{
    public bool IsEmpty => string.IsNullOrWhiteSpace(Html);
}
