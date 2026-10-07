using DocumentExport.Api.Exports;
using DocumentExport.Contracts;
using Npgsql;

namespace DocumentExport.Api.Persistence;

/// <summary>Postgres implementation of <see cref="IExportStore"/>.</summary>
public sealed class ExportStore(NpgsqlDataSource dataSource, ILogger<ExportStore> logger) : IExportStore
{
    public async Task<IReadOnlyList<InventoryRow>> ReadInventoryAsync(
        string warehouseCode, bool includeZeroStock, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT sku, description, quantity, bin_location
            FROM inventory_items
            WHERE warehouse_code = @warehouse AND (@include_zero OR quantity > 0)
            ORDER BY sku
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("warehouse", warehouseCode);
        command.Parameters.AddWithValue("include_zero", includeZeroStock);

        var rows = new List<InventoryRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new InventoryRow(reader.GetString(0), reader.GetString(1), reader.GetInt32(2), reader.GetString(3)));
        }

        logger.LogDebug("Read {Count} inventory rows for {Warehouse}", rows.Count, warehouseCode);
        return rows;
    }

    public async Task InsertAsync(ExportJob job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        const string sql = """
            INSERT INTO export_jobs (id, warehouse_code, status, object_key, manifest_sha256, requested_by, created_at)
            VALUES (@id, @warehouse, @status, @object_key, @sha256, @requested_by, @created_at)
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", job.Id);
        command.Parameters.AddWithValue("warehouse", job.WarehouseCode);
        command.Parameters.AddWithValue("status", (int)job.Status);
        command.Parameters.AddWithValue("object_key", job.ObjectKey);
        command.Parameters.AddWithValue("sha256", (object?)job.ManifestSha256 ?? DBNull.Value);
        command.Parameters.AddWithValue("requested_by", job.RequestedBy);
        command.Parameters.AddWithValue("created_at", job.CreatedAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(Guid exportId, ExportStatus status, string? manifestSha256, CancellationToken cancellationToken)
    {
        const string sql = "UPDATE export_jobs SET status = @status, manifest_sha256 = @sha256 WHERE id = @id";

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", exportId);
        command.Parameters.AddWithValue("status", (int)status);
        command.Parameters.AddWithValue("sha256", (object?)manifestSha256 ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<ExportJob?> FindAsync(Guid exportId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, warehouse_code, status, object_key, manifest_sha256, requested_by, created_at
            FROM export_jobs
            WHERE id = @id
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", exportId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ExportJob(
            reader.GetGuid(0),
            reader.GetString(1),
            (ExportStatus)reader.GetInt32(2),
            reader.GetString(3),
            await reader.IsDBNullAsync(4, cancellationToken) ? null : reader.GetString(4),
            reader.GetString(5),
            reader.GetFieldValue<DateTimeOffset>(6));
    }
}
