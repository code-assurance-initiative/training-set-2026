namespace FleetOps.Infrastructure.Auditing;

public sealed class AuditOptions
{
    public const string SectionName = "Audit";

    public AuditRetentionPolicy Retention { get; set; } = new();
}

public sealed class AuditRetentionPolicy
{
    public int KeepDays { get; set; } = 2_557;
}

public sealed record AuditQuery(string EntityType, string EntityId);
