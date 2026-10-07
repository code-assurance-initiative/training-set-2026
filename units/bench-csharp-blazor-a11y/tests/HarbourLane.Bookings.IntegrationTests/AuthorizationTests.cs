using System.Net;

namespace HarbourLane.Bookings.IntegrationTests;

public sealed class AuthorizationTests(PortalFactory factory) : IClassFixture<PortalFactory>
{
    [Theory]
    [InlineData("/Admin")]
    [InlineData("/Admin/Notice")]
    public async Task The_staff_area_challenges_anonymous_visitors(string path)
    {
        using var client = factory.CreatePortalClient();

        var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith(PortalFactory.AuthorizeEndpoint, response.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Signing_out_requires_a_signed_in_user()
    {
        using var client = factory.CreatePortalClient();

        using var response = await client.PostAsync(new Uri("/account/logout", UriKind.Relative), content: null, TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
