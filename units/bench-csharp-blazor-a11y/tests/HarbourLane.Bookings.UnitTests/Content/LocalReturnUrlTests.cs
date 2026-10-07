using HarbourLane.Bookings.Web.Security;

namespace HarbourLane.Bookings.UnitTests.Content;

public sealed class LocalReturnUrlTests
{
    [Theory]
    [InlineData("/bookings", "/bookings")]
    [InlineData("/book/workshop?x=1", "/book/workshop?x=1")]
    [InlineData(null, "/")]
    [InlineData("", "/")]
    [InlineData("https://elsewhere.org/", "/")]
    [InlineData("//elsewhere.org/", "/")]
    [InlineData("/\\elsewhere.org", "/")]
    public void Only_local_paths_are_kept(string? returnUrl, string expected)
    {
        Assert.Equal(expected, AccountEndpoints.LocalOnly(returnUrl));
    }
}
