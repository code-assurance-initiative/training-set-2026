using System.Net;
using FluentAssertions;
using Invoicing.Api.Branding;
using Microsoft.Extensions.Logging.Abstractions;

namespace Invoicing.UnitTests.Branding;

public sealed class BrandingServiceClientTests
{
    [Fact]
    public async Task ReturnsTheTenantLogo()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) });

        var logo = await Client(handler).GetLogoAsync("fjord-logistics", TestContext.Current.CancellationToken);

        logo.Should().Equal(1, 2, 3);
        handler.Requested.Should().ContainSingle().Which.Should().Be(new Uri("https://branding.test/tenants/fjord-logistics/logo"));
    }

    [Fact]
    public async Task ATenantWithoutALogoGetsNone()
    {
        var logo = await Client(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound))).GetLogoAsync("fjord-logistics", TestContext.Current.CancellationToken);

        logo.Should().BeNull();
    }

    [Fact]
    public async Task AnOversizedLogoIsIgnored()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[(512 * 1024) + 1]) });

        var logo = await Client(handler).GetLogoAsync("fjord-logistics", TestContext.Current.CancellationToken);

        logo.Should().BeNull();
    }

    [Fact]
    public async Task AServerErrorIsReported()
    {
        var act = () => Client(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway))).GetLogoAsync("fjord-logistics", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    private static BrandingServiceClient Client(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://branding.test/") }, NullLogger<BrandingServiceClient>.Instance);

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<Uri?> Requested { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requested.Add(request.RequestUri);
            return Task.FromResult(respond(request));
        }
    }
}
