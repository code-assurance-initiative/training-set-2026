using System.Net;
using System.Net.Http.Json;
using DocumentExport.Contracts;

namespace DocumentExport.IntegrationTests;

public sealed class ExportEndpointsTests(ExportApiFactory factory) : IClassFixture<ExportApiFactory>
{
    private CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Health_is_public_and_carries_the_security_headers()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative), Cancellation);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Contains("default-src 'none'", response.Headers.GetValues("Content-Security-Policy").Single(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Creating_an_export_requires_a_caller_identity()
    {
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/exports", new ExportRequest { WarehouseCode = "OSL01" }, Cancellation);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Creating_an_export_requires_the_write_role()
    {
        using var client = Caller("Exports.Read");

        using var response = await client.PostAsJsonAsync("/exports", new ExportRequest { WarehouseCode = "OSL01" }, Cancellation);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task An_invalid_warehouse_code_is_a_validation_problem()
    {
        using var client = Caller("Exports.Write");

        using var response = await client.PostAsJsonAsync("/exports", new ExportRequest { WarehouseCode = "osl-1" }, Cancellation);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task An_export_can_be_downloaded_once_with_its_token()
    {
        using var caller = Caller("Exports.Write");
        using var created = await caller.PostAsJsonAsync("/exports", new ExportRequest { WarehouseCode = "OSL01" }, Cancellation);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var export = await created.Content.ReadFromJsonAsync<ExportResponse>(Cancellation);
        Assert.NotNull(export);
        Assert.Equal(ExportStatus.Ready, export.Status);

        using var tokenResponse = await caller.PostAsync(new Uri($"/exports/{export.ExportId:D}/download-token", UriKind.Relative), null, Cancellation);
        var token = await tokenResponse.Content.ReadFromJsonAsync<DownloadTokenResponse>(Cancellation);
        Assert.NotNull(token);

        using var downloader = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/downloads/{export.ExportId:D}");
        request.Headers.Add("X-Download-Token", token.Token);
        using var download = await downloader.SendAsync(request, Cancellation);

        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal(export.ManifestSha256, download.Headers.GetValues("X-Content-SHA256").Single());
        var csv = await download.Content.ReadAsStringAsync(Cancellation);
        Assert.Equal("sku,description,quantity,bin_location\r\nA-100,Pallet wrap,12,R01-S2\r\n", csv);
        Assert.Contains(factory.Audit.Entries, e => e.Action == "export.downloaded" && e.ExportId == export.ExportId);
    }

    [Fact]
    public async Task A_token_for_one_export_does_not_open_another()
    {
        using var caller = Caller("Exports.Write");
        using var created = await caller.PostAsJsonAsync("/exports", new ExportRequest { WarehouseCode = "OSL01" }, Cancellation);
        var export = await created.Content.ReadFromJsonAsync<ExportResponse>(Cancellation);
        Assert.NotNull(export);
        using var tokenResponse = await caller.PostAsync(new Uri($"/exports/{export.ExportId:D}/download-token", UriKind.Relative), null, Cancellation);
        var token = await tokenResponse.Content.ReadFromJsonAsync<DownloadTokenResponse>(Cancellation);
        Assert.NotNull(token);

        using var downloader = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/downloads/{Guid.NewGuid():D}");
        request.Headers.Add("X-Download-Token", token.Token);
        using var download = await downloader.SendAsync(request, Cancellation);

        Assert.Equal(HttpStatusCode.Forbidden, download.StatusCode);
    }

    private HttpClient Caller(string roles)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Roles", roles);
        return client;
    }
}
