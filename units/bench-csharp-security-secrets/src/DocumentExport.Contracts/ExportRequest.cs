using System.ComponentModel.DataAnnotations;

namespace DocumentExport.Contracts;

/// <summary>Asks the service to export the current inventory of one warehouse.</summary>
public sealed record ExportRequest
{
    /// <summary>Warehouse code: three upper-case letters followed by two digits, e.g. <c>OSL01</c>.</summary>
    [Required]
    [RegularExpression("^[A-Z]{3}[0-9]{2}$")]
    public string WarehouseCode { get; init; } = string.Empty;

    /// <summary>Whether items with zero stock are listed.</summary>
    public bool IncludeZeroStock { get; init; }
}
