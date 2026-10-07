using DocumentExport.Api.Exports;
using DocumentExport.Contracts;

namespace DocumentExport.Api.Persistence;

/// <summary>Export jobs and the inventory they are rendered from.</summary>
public interface IExportStore
{
    Task<IReadOnlyList<InventoryRow>> ReadInventoryAsync(string warehouseCode, bool includeZeroStock, CancellationToken cancellationToken);

    Task InsertAsync(ExportJob job, CancellationToken cancellationToken);

    Task UpdateAsync(Guid exportId, ExportStatus status, string? manifestSha256, CancellationToken cancellationToken);

    Task<ExportJob?> FindAsync(Guid exportId, CancellationToken cancellationToken);
}
