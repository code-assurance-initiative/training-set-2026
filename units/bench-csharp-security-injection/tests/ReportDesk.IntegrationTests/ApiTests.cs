using System.Net;
using System.Net.Http.Json;
using ReportDesk.Api.Documents;
using ReportDesk.Api.Reports;
using ReportDesk.Api.Search;

namespace ReportDesk.IntegrationTests;

public sealed class ApiTests(ReportDeskApiFactory factory) : IClassFixture<ReportDeskApiFactory>
{
    private const string MetadataXml =
        "<metadata><title>Board minutes &lt;Q3&gt;</title><pages>4</pages><field name=\"case\" label=\"Case\">C-7</field></metadata>";

    private static readonly Guid MinutesId = Guid.Parse("0f8a3c52-9a41-4f0e-8a7c-3e1f5b2d9c11");

    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    private void SeedMinutes()
    {
        if (factory.Archive.Documents.TrueForAll(document => document.Id != MinutesId))
        {
            factory.Archive.Documents.Add(new DocumentRecord(
                MinutesId, "BRD-2026-17", "Board minutes <Q3>", "k.holm", "Minutes. Invoice 4411 approved.", MetadataXml, DateTimeOffset.UnixEpoch));
        }
    }

    [Fact]
    public async Task HealthIsAnonymous()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative), Cancel);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EndpointsRequireAToken()
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(new Uri("/search", UriKind.Relative), new { term = "board" }, Cancel);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EndpointsRequireTheirScope()
    {
        using var client = factory.CreateClient("reports.read");
        using var response = await client.PostAsJsonAsync(new Uri("/search", UriKind.Relative), new { term = "board" }, Cancel);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ResponsesCarrySecurityHeaders()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative), Cancel);
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Contains("frame-ancestors 'none'", Assert.Single(response.Headers.GetValues("Content-Security-Policy")), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SearchAppliesTheFilterTree()
    {
        SeedMinutes();
        using var client = factory.CreateClient("documents.read");
        var request = new { term = "minutes", filter = new { or = new object[] { new { field = "owner", equals = "k.holm" } } } };
        using var response = await client.PostAsJsonAsync(new Uri("/search", UriKind.Relative), request, Cancel);

        response.EnsureSuccessStatusCode();
        var hits = await response.Content.ReadFromJsonAsync<List<DocumentHit>>(Cancel);
        Assert.Equal(MinutesId, Assert.Single(hits ?? []).Id);
    }

    [Fact]
    public async Task SearchRejectsAnUnknownFilter()
    {
        using var client = factory.CreateClient("documents.read");
        using var response = await client.PostAsJsonAsync(
            new Uri("/search", UriKind.Relative), new { term = "minutes", filter = new { field = "title", equals = "x" } }, Cancel);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchWidgetEncodesDocumentTitles()
    {
        SeedMinutes();
        using var client = factory.CreateClient("documents.read");
        var html = await client.GetStringAsync(new Uri("/widgets/search?q=minutes", UriKind.Relative), Cancel);

        Assert.Contains("Board minutes &lt;Q3&gt;", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<Q3>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DocumentCardHasAnETag()
    {
        SeedMinutes();
        using var client = factory.CreateClient("documents.read");
        using var response = await client.GetAsync(new Uri($"/documents/{MinutesId}/card", UriKind.Relative), Cancel);

        response.EnsureSuccessStatusCode();
        Assert.NotNull(response.Headers.ETag);
        Assert.Contains("data-pages=\"4\"", await response.Content.ReadAsStringAsync(Cancel), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DocumentListRejectsAMalformedNumber()
    {
        using var client = factory.CreateClient("documents.read");
        using var response = await client.GetAsync(new Uri("/documents?number=nope", UriKind.Relative), Cancel);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MetadataFieldsAreReadByName()
    {
        SeedMinutes();
        using var client = factory.CreateClient("documents.read");
        var values = await client.GetFromJsonAsync<List<string>>(new Uri($"/metadata/{MinutesId}/fields/case", UriKind.Relative), Cancel);
        Assert.Equal(["C-7"], values);
    }

    [Fact]
    public async Task SavedSearchesAreImportedAndDeleted()
    {
        using var client = factory.CreateClient("documents.write");
        const string file = """[{"Name":"overdue","Term":"invoice","Parameters":{}}]""";
        using var import = await client.PostAsync(new Uri("/saved-searches/import", UriKind.Relative), new StringContent(file), Cancel);
        Assert.Equal(1, await import.Content.ReadFromJsonAsync<int>(Cancel));

        using var delete = await client.DeleteAsync(new Uri("/saved-searches/overdue", UriKind.Relative), Cancel);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Empty(factory.SavedSearches.Saved);
    }

    [Fact]
    public async Task GroupLookupsReachTheDirectoryEscaped()
    {
        using var client = factory.CreateClient("documents.read");
        using var response = await client.GetAsync(new Uri("/people/groups/fin*", UriKind.Relative), Cancel);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains(@"(&(objectClass=groupOfNames)(cn=fin\2a))", factory.Directory.Filters, StringComparer.Ordinal);
    }

    [Fact]
    public async Task DeliveryMailsTheSubscriber()
    {
        var report = new ReportDefinition { Id = Guid.NewGuid(), Name = "Weekly", Owner = "k.holm" };
        var subscriber = new Subscriber { Id = Guid.NewGuid(), ReportId = report.Id, EmailAddress = "ann.berg@archive.test" };
        factory.Reports.Reports.Add(report);
        factory.Reports.Subscribers.Add(subscriber);

        using var client = factory.CreateClient("reports.write");
        using var response = await client.PostAsJsonAsync(
            new Uri("/deliveries", UriKind.Relative), new { reportId = report.Id, subscriberId = subscriber.Id }, Cancel);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Contains(factory.Mail.Sent, mail => mail.To == "ann.berg@archive.test" && mail.Body.Contains("Ann Berg", StringComparison.Ordinal));
    }
}
