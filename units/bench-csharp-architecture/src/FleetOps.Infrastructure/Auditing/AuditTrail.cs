using FleetOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FleetOps.Infrastructure.Auditing;

public sealed class AuditTrail(FleetOpsDbContext db, IOptions<AuditOptions> options, TimeProvider clock) : IAuditTrail
{
    public async Task<IReadOnlyList<AuditEntry>> HistoryAsync(AuditQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await db.AuditEntries.AsNoTracking()
            .Where(a => a.EntityType == query.EntityType && a.EntityId == query.EntityId)
            .OrderBy(a => a.At)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<int> PurgeExpiredAsync(CancellationToken cancellationToken)
    {
        var cutoff = clock.GetUtcNow().AddDays(-options.Value.Retention.KeepDays);
        return db.AuditEntries.Where(a => a.At < cutoff).ExecuteDeleteAsync(cancellationToken);
    }
}
