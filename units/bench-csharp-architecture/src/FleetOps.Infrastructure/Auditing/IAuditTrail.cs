namespace FleetOps.Infrastructure.Auditing;

public interface IAuditTrail
{
    Task<IReadOnlyList<AuditEntry>> HistoryAsync(AuditQuery query, CancellationToken cancellationToken);

    Task<int> PurgeExpiredAsync(CancellationToken cancellationToken);
}
