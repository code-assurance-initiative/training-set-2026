using System.Net;
using System.Text;

namespace FleetOps.Application.Tests.Support;

/// <summary>Answers every request with a canned JSON body and remembers what was asked.</summary>
public sealed class StubHttp(HttpStatusCode status, string json) : HttpMessageHandler
{
    public List<string> Requests { get; } = [];

    public static HttpClient Client(StubHttp handler) => new(handler) { BaseAddress = new Uri("https://vendor.test/api/") };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add($"{request.Method} {request.RequestUri?.PathAndQuery}");
        return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
    }
}
