using System.Net;

namespace ParcelTracking.IntegrationTests;

public sealed class PlatformEndpointsTests(TrackingApiFactory factory) : IClassFixture<TrackingApiFactory>
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Health_endpoints_answer_without_a_token(string path)
    {
        var response = await factory.CreateClient(merchantId: null).GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Security_txt_is_public()
    {
        var response = await factory.CreateClient(merchantId: null).GetAsync("/.well-known/security.txt", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("Contact: https://", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Api_responses_carry_the_security_headers()
    {
        var client = factory.CreateClient("merchant-a", ApiTestData.Read);

        var response = await client.GetAsync("/api/parcels/NP000000001", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
        Assert.Contains("frame-ancestors 'none'", Assert.Single(response.Headers.GetValues("Content-Security-Policy")), StringComparison.Ordinal);
    }
}
