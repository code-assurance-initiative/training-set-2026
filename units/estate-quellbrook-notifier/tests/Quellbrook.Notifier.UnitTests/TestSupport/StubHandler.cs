using System.Net;

namespace Quellbrook.Notifier.UnitTests.TestSupport;

/// <summary>Answers every request with a fixed status and records what was sent.</summary>
internal sealed class StubHandler(HttpStatusCode status, params (string Name, string Value)[] headers) : HttpMessageHandler
{
    public List<(HttpRequestMessage Request, string Body)> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add((request, body));
        var response = new HttpResponseMessage(status);
        foreach (var (name, value) in headers)
        {
            response.Headers.Add(name, value);
        }

        return response;
    }
}
