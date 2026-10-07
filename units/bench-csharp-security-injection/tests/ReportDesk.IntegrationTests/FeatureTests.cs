using System.Net;
using System.Net.Http.Json;
using System.Text;
using ReportDesk.Api.Documents;
using ReportDesk.Api.Importing;
using ReportDesk.Api.Metadata;
using ReportDesk.Api.Reports;

namespace ReportDesk.IntegrationTests;

public sealed class FeatureTests(ReportDeskApiFactory factory) : IClassFixture<ReportDeskApiFactory>
{
    private const string MetadataXml =
        "<metadata><title>Lease</title><pages>2</pages><field name=\"case\" label=\"Case\">L-9</field>" +
        "<parties><field name=\"tenant\">North Ltd</field></parties></metadata>";

    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    private Guid SeedDocument()
    {
        var id = Guid.NewGuid();
        factory.Archive.Documents.Add(new DocumentRecord(id, "LEA-2026-3", "Lease", "a.berg", "Lease text.", MetadataXml, DateTimeOffset.UnixEpoch));
        return id;
    }

    private ReportDefinition SeedReport(string owner = "k.holm")
    {
        var report = new ReportDefinition { Id = Guid.NewGuid(), Name = "Monthly", Owner = owner, TemplateName = "monthly" };
        factory.Reports.Reports.Add(report);
        return report;
    }

    [Fact]
    public async Task DownloadsAnAttachment()
    {
        var documentId = Guid.NewGuid();
        var directory = Directory.CreateDirectory(Path.Combine(factory.Root, "attachments", documentId.ToString("N")));
        await File.WriteAllBytesAsync(Path.Combine(directory.FullName, "scan.pdf"), [37, 80, 68, 70], Cancel);

        using var client = factory.CreateClient("documents.read");
        var bytes = await client.GetByteArrayAsync(new Uri($"/documents/{documentId}/attachments/scan.pdf", UriKind.Relative), Cancel);
        using var missing = await client.GetAsync(new Uri($"/documents/{documentId}/attachments/other.pdf", UriKind.Relative), Cancel);

        Assert.Equal([37, 80, 68, 70], bytes);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task PullsAConfiguredPartnerFeedOnly()
    {
        using var client = factory.CreateClient("reportdesk.admin");
        using var allowed = await client.PostAsJsonAsync(new Uri("/feeds/pull", UriKind.Relative), new { feedUrl = "https://records.partner.test/feed" }, Cancel);
        using var refused = await client.PostAsJsonAsync(new Uri("/feeds/pull", UriKind.Relative), new { feedUrl = "https://elsewhere.test/feed" }, Cancel);

        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
    }

    [Fact]
    public async Task PreviewsAnImport()
    {
        using var client = factory.CreateClient("documents.write");
        var preview = await client.GetFromJsonAsync<ImportPreview>(
            new Uri("/imports/preview?sourceUrl=https%3A%2F%2Fdocs.vendor.test%2Fcontract.pdf", UriKind.Relative), Cancel);

        Assert.Equal(200, preview?.StatusCode);
        Assert.Equal("application/json", preview?.ContentType);
    }

    [Fact]
    public async Task ImportsABundle()
    {
        using var bundle = new MemoryStream();
        using (var writer = new BinaryWriter(bundle, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(BundleIndexReader.Magic);
            writer.Write((ushort)1);
            writer.Write(7);
            writer.Write("a.txt.1"u8);
            writer.Write(2);
            writer.Write("hi"u8);
        }

        using var form = new MultipartFormDataContent { { new ByteArrayContent(bundle.ToArray()), "bundle", "export.rdb" } };
        using var client = factory.CreateClient("documents.write");
        using var response = await client.PostAsync(new Uri("/imports/bundles", UriKind.Relative), form, Cancel);

        response.EnsureSuccessStatusCode();
        Assert.Equal(["a.txt.1"], await response.Content.ReadFromJsonAsync<List<string>>(Cancel));
    }

    [Fact]
    public async Task RejectsSomethingThatIsNotABundle()
    {
        using var form = new MultipartFormDataContent { { new ByteArrayContent("hello"u8.ToArray()), "bundle", "export.rdb" } };
        using var client = factory.CreateClient("documents.write");
        using var response = await client.PostAsync(new Uri("/imports/bundles", UriKind.Relative), form, Cancel);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TestsAWebhook()
    {
        using var client = factory.CreateClient("reports.write");
        using var https = await client.PostAsJsonAsync(new Uri("/webhooks/test", UriKind.Relative), new { callbackUrl = "https://hooks.tenant.test/in" }, Cancel);
        using var http = await client.PostAsJsonAsync(new Uri("/webhooks/test", UriKind.Relative), new { callbackUrl = "http://hooks.tenant.test/in" }, Cancel);

        Assert.True(await https.Content.ReadFromJsonAsync<bool>(Cancel));
        Assert.Equal(HttpStatusCode.BadRequest, http.StatusCode);
    }

    [Fact]
    public async Task ImportsAndFlattensMetadata()
    {
        using var client = factory.CreateClient("documents.read", "documents.write");
        using var imported = await client.PostAsync(new Uri("/metadata/import", UriKind.Relative), new StringContent(MetadataXml), Cancel);
        using var flattened = await client.PostAsync(new Uri("/metadata/flatten", UriKind.Relative), new StringContent(MetadataXml), Cancel);
        using var broken = await client.PostAsync(new Uri("/metadata/flatten", UriKind.Relative), new StringContent("<metadata>"), Cancel);

        var fields = await imported.Content.ReadFromJsonAsync<List<MetadataField>>(Cancel);
        Assert.Equal(["case", "tenant"], fields?.Select(field => field.Name));
        Assert.Contains(new MetadataField("metadata/title", "Lease"), await flattened.Content.ReadFromJsonAsync<List<MetadataField>>(Cancel) ?? []);
        Assert.Equal(HttpStatusCode.BadRequest, broken.StatusCode);
    }

    [Fact]
    public async Task ReadsSectionsAndRendersAPreview()
    {
        var id = SeedDocument();
        using var client = factory.CreateClient("documents.read");

        var section = await client.GetFromJsonAsync<List<MetadataField>>(new Uri($"/metadata/{id}/sections/parties", UriKind.Relative), Cancel);
        var preview = await client.GetStringAsync(new Uri($"/metadata/{id}/preview", UriKind.Relative), Cancel);
        using var unknown = await client.GetAsync(new Uri($"/metadata/{Guid.NewGuid()}/preview", UriKind.Relative), Cancel);

        Assert.Equal(new MetadataField("tenant", "North Ltd"), Assert.Single(section ?? []));
        Assert.Contains("<dd title=\"L-9\">L-9</dd>", preview, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Fact]
    public async Task ReadsARetentionSchedule()
    {
        using var client = factory.CreateClient("documents.read", "reportdesk.admin");
        using var response = await client.PostAsync(
            new Uri("/metadata/retention", UriKind.Relative), new StringContent("<retention><rule class=\"HR\" years=\"7\"/></retention>"), Cancel);

        Assert.Equal(7, (await response.Content.ReadFromJsonAsync<Dictionary<string, int>>(Cancel))?["HR"]);
    }

    [Fact]
    public async Task ListsReportsAndDueSchedules()
    {
        var report = SeedReport("m.dahl");
        using var client = factory.CreateClient("reports.read");

        var reports = await client.GetFromJsonAsync<List<ReportDefinition>>(new Uri("/reports?owner=m.dahl", UriKind.Relative), Cancel);
        var due = await client.GetFromJsonAsync<List<ReportSchedule>>(new Uri("/reports/schedules/due?owner=m.dahl", UriKind.Relative), Cancel);

        Assert.Equal(report.Id, Assert.Single(reports ?? []).Id);
        Assert.Empty(due ?? []);
    }

    [Fact]
    public async Task SavesAValidLayoutAndRejectsAnInvalidOne()
    {
        var report = SeedReport();
        using var client = factory.CreateClient("reports.read", "reports.write");

        using var saved = await client.PutAsync(new Uri($"/reports/{report.Id}/layout", UriKind.Relative), new StringContent("""{"PageSize":"A3"}"""), Cancel);
        using var invalid = await client.PutAsync(new Uri($"/reports/{report.Id}/layout", UriKind.Relative), new StringContent("""{"Script":1}"""), Cancel);

        Assert.Equal(HttpStatusCode.NoContent, saved.StatusCode);
        Assert.Contains("\"PageSize\":\"A3\"", report.LayoutJson, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task ExportsAReport()
    {
        var report = SeedReport();
        await File.WriteAllTextAsync(Path.Combine(factory.Root, "templates", "monthly.html"), "<h1>Monthly</h1>", Cancel);
        using var client = factory.CreateClient("reports.read", "reports.write");

        using var accepted = await client.PostAsync(new Uri($"/reports/{report.Id}/exports?format=pdf", UriKind.Relative), null, Cancel);
        using var redirected = await client.PostAsync(
            new Uri($"/reports/{report.Id}/exports?format=pdf&returnUrl=%2Freports%2Fdone", UriKind.Relative), null, Cancel);
        using var missing = await client.PostAsync(new Uri($"/reports/{Guid.NewGuid()}/exports?format=pdf", UriKind.Relative), null, Cancel);

        Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, redirected.StatusCode);
        Assert.Equal("/reports/done", redirected.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.True(File.Exists(Path.Combine(factory.Root, "scratch", $"{report.Id:N}.html")));
    }

    [Fact]
    public async Task RecordsABounceAndSwitchesTheEmailChannel()
    {
        var subscriber = new Subscriber { Id = Guid.NewGuid(), EmailAddress = "per.lund@archive.test" };
        factory.Reports.Subscribers.Add(subscriber);
        using var client = factory.CreateClient("reports.write");

        using var bounce = await client.PostAsJsonAsync(new Uri("/deliveries/bounces", UriKind.Relative), new { subscriberId = subscriber.Id, reason = "mailbox full" }, Cancel);
        using var channel = await client.PutAsJsonAsync(new Uri($"/subscriptions/{subscriber.Id}/channels/email", UriKind.Relative), new { enabled = false }, Cancel);

        Assert.Equal(HttpStatusCode.NoContent, bounce.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, channel.StatusCode);
        Assert.False(subscriber.EmailEnabled);
        Assert.Contains(factory.Crm.Requests, request => request.StartsWith("PUT /api/subscriptions/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task LooksUpOwners()
    {
        using var client = factory.CreateClient("documents.read");
        var owners = await client.GetFromJsonAsync<List<object>>(new Uri("/people/owners?email=a.berg%40archive.test", UriKind.Relative), Cancel);
        Assert.Empty(owners ?? []);
    }
}
