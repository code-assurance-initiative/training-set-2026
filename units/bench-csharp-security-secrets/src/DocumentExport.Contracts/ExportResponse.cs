namespace DocumentExport.Contracts;

/// <summary>The state of one export.</summary>
/// <param name="ExportId">Identifier of the export.</param>
/// <param name="WarehouseCode">Warehouse the export lists.</param>
/// <param name="Status">Current lifecycle state.</param>
/// <param name="CreatedAt">When the export was requested.</param>
/// <param name="ManifestSha256">Hex SHA-256 of the rendered document, once it is ready.</param>
public sealed record ExportResponse(
    Guid ExportId,
    string WarehouseCode,
    ExportStatus Status,
    DateTimeOffset CreatedAt,
    string? ManifestSha256);
