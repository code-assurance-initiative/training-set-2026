using Microsoft.EntityFrameworkCore;

namespace ReportDesk.Api.Reports;

public sealed class SubscriberStore(ReportsDbContext db) : ISubscriberStore
{
    public Task<Subscriber?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.Subscribers.AsNoTracking().FirstOrDefaultAsync(subscriber => subscriber.Id == id, cancellationToken);

    public async Task SetEmailEnabledAsync(Guid id, bool enabled, CancellationToken cancellationToken)
    {
        await db.Subscribers
            .Where(subscriber => subscriber.Id == id)
            .ExecuteUpdateAsync(set => set.SetProperty(subscriber => subscriber.EmailEnabled, enabled), cancellationToken)
            .ConfigureAwait(false);
    }
}
