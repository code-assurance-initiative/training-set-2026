using System.Security.Cryptography;
using DocumentExport.Api.Encryption;
using DocumentExport.Api.Notifications;
using DocumentExport.Api.Partner;
using DocumentExport.Api.Persistence;
using DocumentExport.Api.Signing;
using DocumentExport.Api.Storage;
using DocumentExport.Contracts;

namespace DocumentExport.Api.Exports;

/// <summary>A decrypted export ready to stream to the caller.</summary>
public sealed record ExportDownload(string FileName, byte[] Content, ExportManifest Manifest);

/// <summary>Renders, encrypts, stores and announces exports; serves them back for download.</summary>
public sealed class ExportService(
    IExportStore store,
    IObjectStore objectStore,
    IAuditLog audit,
    IPartnerApi partner,
    IDeliveryNotifier notifier,
    ExportEncryptor encryptor,
    ManifestSigner signer,
    TimeProvider timeProvider,
    ILogger<ExportService> logger)
{
    public async Task<ExportResponse> CreateAsync(ExportRequest request, string clientApplication, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var rows = await store.ReadInventoryAsync(request.WarehouseCode, request.IncludeZeroStock, cancellationToken);
        var document = InventoryCsvWriter.Write(rows);
        var manifest = signer.Sign(document);

        var id = Guid.NewGuid();
        var job = new ExportJob(id, request.WarehouseCode, ExportStatus.Pending,
            $"inventory/{request.WarehouseCode}/{id:N}.csv.enc", null, clientApplication, timeProvider.GetUtcNow());
        await store.InsertAsync(job, cancellationToken);

        try
        {
            await objectStore.UploadAsync(job.ObjectKey, encryptor.Encrypt(document), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Export {ExportId} could not be stored", id);
            await store.UpdateAsync(id, ExportStatus.Failed, null, CancellationToken.None);
            throw;
        }

        job = job with { Status = ExportStatus.Ready, ManifestSha256 = manifest.Sha256 };
        await store.UpdateAsync(id, job.Status, job.ManifestSha256, cancellationToken);
        await audit.RecordAsync("export.created", id, clientApplication, cancellationToken);
        await AnnounceAsync(job, cancellationToken);

        logger.LogInformation("Export {ExportId} of {Rows} rows for {Warehouse} is ready", id, rows.Count, job.WarehouseCode);
        return job.ToResponse();
    }

    public async Task<ExportResponse?> FindAsync(Guid exportId, CancellationToken cancellationToken)
    {
        var job = await store.FindAsync(exportId, cancellationToken);
        return job?.ToResponse();
    }

    public async Task<ExportDownload?> OpenAsync(Guid exportId, CancellationToken cancellationToken)
    {
        var job = await store.FindAsync(exportId, cancellationToken);
        if (job is not { Status: ExportStatus.Ready, ManifestSha256: { } expectedSha256 })
        {
            return null;
        }

        var document = encryptor.Decrypt(await objectStore.DownloadAsync(job.ObjectKey, cancellationToken));
        var manifest = signer.Sign(document);
        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(manifest.Sha256), Convert.FromHexString(expectedSha256)))
        {
            throw new CryptographicException($"Export {exportId:D} does not match its manifest.");
        }

        await audit.RecordAsync("export.downloaded", exportId, "download-token", cancellationToken);
        return new ExportDownload($"inventory-{job.WarehouseCode}-{job.CreatedAt:yyyyMMdd}.csv", document, manifest);
    }

    private async Task AnnounceAsync(ExportJob job, CancellationToken cancellationToken)
    {
        try
        {
            await partner.AnnounceExportAsync(job, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "The fulfilment partner was not told about export {ExportId}; it can fetch it later", job.Id);
        }

        try
        {
            await notifier.ExportReadyAsync(job, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "No notification was sent for export {ExportId}", job.Id);
        }
    }
}
