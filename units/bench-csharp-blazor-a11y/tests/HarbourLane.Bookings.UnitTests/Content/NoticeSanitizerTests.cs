using HarbourLane.Bookings.Web.Content;

namespace HarbourLane.Bookings.UnitTests.Content;

public sealed class NoticeSanitizerTests
{
    private readonly NoticeSanitizer _sanitizer = new();

    [Fact]
    public void Formatting_and_https_links_are_kept()
    {
        const string html = "<p><strong>Closed</strong> on Monday. <a href=\"https://harbourlane.org/hours\">Opening hours</a></p>";

        Assert.Equal(html, _sanitizer.Sanitize(html));
    }

    [Theory]
    [InlineData("<p>Hi<script>alert(1)</script></p>", "<p>Hi</p>")]
    [InlineData("<p onclick=\"steal()\">Hi</p>", "<p>Hi</p>")]
    [InlineData("<a href=\"javascript:steal()\">Hi</a>", "<a>Hi</a>")]
    [InlineData("<img src=x onerror=steal()>", "")]
    public void Anything_executable_is_removed(string html, string expected)
    {
        Assert.Equal(expected, _sanitizer.Sanitize(html));
    }
}
