using System.ComponentModel.DataAnnotations;

namespace DocumentExport.Api.Persistence;

/// <summary>Connection settings of the audit database (bound from the "AuditStore" section).</summary>
public sealed class AuditStoreSettings
{
    public const string SectionName = "AuditStore";

    /// <summary>Falls back to the shared audit database when the section does not set one.</summary>
    [Required]
    public string ConnectionString { get; set; } =
        "Host=audit-db.internal;Port=5432;Database=audit;Username=export_audit;Password=D9z7f$idj&MLc5EvZe220$;SSL Mode=Require";

    [Range(1, 120)]
    public int CommandTimeoutSeconds { get; set; } = 15;
}
