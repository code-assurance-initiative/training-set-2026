using System.Net;
using ReportDesk.Api.Feeds;

namespace ReportDesk.UnitTests.Security;

public sealed class PartnerFeedClientTests : IDisposable
{
    private readonly StubHandler _handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]") });
    private readonly HttpClient _http;
    private readonly PartnerFeedClient _client;

    public PartnerFeedClientTests()
    {
        _http = new HttpClient(_handler);
        _client = new PartnerFeedClient(_http, TestOptions.Feeds("records.partner.test"));
    }

    [Fact]
    public async Task FetchesAConfiguredPartnerFeed()
    {
        var feed = await _client.FetchAsync("https://RECORDS.partner.test/v2/feed", TestContext.Current.CancellationToken);
        Assert.Equal("[]", feed);
        Assert.Single(_handler.Requests);
    }

    [Theory]
    [InlineData("https://records.partner.test.evil.test/feed")]
    [InlineData("http://records.partner.test/feed")]
    [InlineData("https://169.254.169.254/latest/meta-data")]
    [InlineData("not a url")]
    public async Task RefusesAnythingElseWithoutSendingARequest(string url)
    {
        await Assert.ThrowsAsync<FeedNotAllowedException>(() => _client.FetchAsync(url, TestContext.Current.CancellationToken));
        Assert.Empty(_handler.Requests);
    }

    public void Dispose()
    {
        _http.Dispose();
        _handler.Dispose();
    }
}
