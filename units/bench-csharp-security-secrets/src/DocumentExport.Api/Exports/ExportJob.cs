using DocumentExport.Contracts;

namespace DocumentExport.Api.Exports;

/// <summary>One requested export and where its encrypted document is stored.</summary>
public sealed record ExportJob(
    Guid Id,
    string WarehouseCode,
    ExportStatus Status,
    string ObjectKey,
    string? ManifestSha256,
    string RequestedBy,
    DateTimeOffset CreatedAt)
{
    public ExportResponse ToResponse() => new(Id, WarehouseCode, Status, CreatedAt, ManifestSha256);
}
