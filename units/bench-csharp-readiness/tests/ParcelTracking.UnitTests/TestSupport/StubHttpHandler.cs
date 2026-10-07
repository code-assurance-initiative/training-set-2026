using System.Net;

namespace ParcelTracking.UnitTests.TestSupport;

/// <summary>Answers every request from a function and records what was sent; nothing leaves the process.</summary>
public sealed class StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<(HttpRequestMessage Request, string? Body)> Requests { get; } = [];

    public static StubHttpHandler Returning(HttpStatusCode status, string? json = null) =>
        new(_ => new HttpResponseMessage(status)
        {
            Content = json is null ? null : new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        });

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add((request, body));
        return respond(request);
    }
}
