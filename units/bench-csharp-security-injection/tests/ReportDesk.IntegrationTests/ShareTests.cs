using System.Net;
using System.Net.Http.Json;
using ReportDesk.Api.Shares;

namespace ReportDesk.IntegrationTests;

public sealed class ShareTests(ReportDeskApiFactory factory) : IClassFixture<ReportDeskApiFactory>
{
    private static readonly Guid DocumentId = Guid.Parse("6d0c2b8e-55a1-4c43-9b8e-2f7d6a1c0e44");

    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    private async Task<string> CreateShareAsync()
    {
        using var client = factory.CreateClient("documents.write");
        using var response = await client.PostAsJsonAsync(
            new Uri("/shares", UriKind.Relative), new { documentId = DocumentId, password = "river-otter-41" }, Cancel);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<ShareCreated>(Cancel);
        return created?.Token ?? throw new InvalidOperationException("No token.");
    }

    private async Task<HttpResponseMessage> OpenAsync(string token, string password, string? returnUrl)
    {
        using var anonymous = factory.CreateClient();
        var form = new Dictionary<string, string> { ["password"] = password };
        if (returnUrl is not null)
        {
            form["returnUrl"] = returnUrl;
        }

        using var content = new FormUrlEncodedContent(form);
        return await anonymous.PostAsync(new Uri($"/shares/{token}/open", UriKind.Relative), content, Cancel);
    }

    [Fact]
    public async Task OpensToALocalReturnUrl()
    {
        var token = await CreateShareAsync();
        using var response = await OpenAsync(token, "river-otter-41", "/shared/inbox?tab=recent");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/shared/inbox?tab=recent", response.Headers.Location?.OriginalString);
    }

    [Theory]
    [InlineData("https://evil.test/login")]
    [InlineData("//evil.test/login")]
    [InlineData("/\\evil.test/login")]
    public async Task IgnoresAReturnUrlOnAnotherHost(string returnUrl)
    {
        var token = await CreateShareAsync();
        using var response = await OpenAsync(token, "river-otter-41", returnUrl);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/shared/{DocumentId:D}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task RefusesAWrongPassword()
    {
        var token = await CreateShareAsync();
        using var response = await OpenAsync(token, "wrong-password", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
