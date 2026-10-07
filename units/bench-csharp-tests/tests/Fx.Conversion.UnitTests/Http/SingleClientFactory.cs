namespace Fx.Conversion.UnitTests.Http;

/// <summary>An <see cref="IHttpClientFactory"/> that hands out clients over one handler, which it does not dispose.</summary>
internal sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}
