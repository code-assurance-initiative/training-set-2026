using DocumentExport.Api.Exports;

namespace DocumentExport.Api.Partner;

/// <summary>Typed HTTP client of the fulfilment partner's inbound-documents API.</summary>
public sealed class PartnerApiClient(HttpClient httpClient, ILogger<PartnerApiClient> logger) : IPartnerApi
{
    public async Task AnnounceExportAsync(ExportJob job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        var announcement = new
        {
            documentId = job.Id,
            kind = "inventory-snapshot",
            warehouse = job.WarehouseCode,
            sha256 = job.ManifestSha256,
            createdAt = job.CreatedAt,
        };

        using var response = await httpClient.PostAsJsonAsync("v2/inbound-documents", announcement, cancellationToken);
        response.EnsureSuccessStatusCode();
        logger.LogInformation("Announced export {ExportId} to the fulfilment partner", job.Id);
    }
}
