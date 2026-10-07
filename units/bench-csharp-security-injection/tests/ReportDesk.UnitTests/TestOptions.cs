using Microsoft.Extensions.Options;
using ReportDesk.Api.Delivery;
using ReportDesk.Api.Feeds;
using ReportDesk.Api.Hosting;
using ReportDesk.Api.People;

namespace ReportDesk.UnitTests;

internal static class TestOptions
{
    public static IOptions<StorageOptions> Storage(string root) => Options.Create(new StorageOptions
    {
        AttachmentsRoot = Path.Combine(root, "attachments"),
        TemplatesRoot = Path.Combine(root, "templates"),
        ScratchRoot = Path.Combine(root, "scratch"),
    });

    public static IOptions<FeedOptions> Feeds(params string[] hosts) => Options.Create(new FeedOptions { AllowedHosts = hosts });

    public static IOptions<PeopleDirectoryOptions> Directory() => Options.Create(new PeopleDirectoryOptions
    {
        Server = "ldap.test",
        BaseDn = "ou=people,dc=test",
        GroupsDn = "ou=groups,dc=test",
    });

    public static IOptions<DeliveryOptions> Delivery(string key) => Options.Create(new DeliveryOptions
    {
        CrmBaseAddress = new Uri("https://crm.test/"),
        SenderAddress = "reports@archive.test",
        PseudonymKey = key,
    });
}

/// <summary>A directory under the system temp path, removed when the test ends.</summary>
internal sealed class TempDirectory : IDisposable
{
    public string Path { get; } = System.IO.Directory.CreateTempSubdirectory("reportdesk-").FullName;

    public void Dispose() => System.IO.Directory.Delete(Path, recursive: true);
}

/// <summary>Answers every request with a fixed response and remembers the requests it saw.</summary>
internal sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<Uri> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is not null)
        {
            Requests.Add(request.RequestUri);
        }

        return Task.FromResult(respond(request));
    }
}
