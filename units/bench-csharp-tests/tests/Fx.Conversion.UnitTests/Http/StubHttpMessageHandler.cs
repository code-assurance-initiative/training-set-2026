using System.Net;
using System.Text;

namespace Fx.Conversion.UnitTests.Http;

/// <summary>Answers every request from <paramref name="respond"/> and records it; nothing leaves the process.</summary>
internal sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    private readonly List<Uri?> _requests = [];

    public IReadOnlyList<Uri?> Requests
    {
        get
        {
            lock (_requests)
            {
                return [.. _requests];
            }
        }
    }

    public static HttpResponseMessage Xml(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/xml") };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        lock (_requests)
        {
            _requests.Add(request.RequestUri);
        }

        return Task.FromResult(respond(request));
    }
}
