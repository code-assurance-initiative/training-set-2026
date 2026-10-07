namespace FleetOps.Infrastructure.Auditing;

public enum AuditAction
{
    Created,
    Modified,
    Deleted,
}

/// <summary>One change to an audited entity, written in the same transaction as the change.</summary>
public sealed class AuditEntry
{
    public Guid Id { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public AuditAction Action { get; set; }

    public DateTimeOffset At { get; set; }
}
