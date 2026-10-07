namespace HarbourLane.Bookings.Content;

public sealed class InMemorySiteNoticeStore(TimeProvider clock) : ISiteNoticeStore
{
    private SiteNotice _current = new(string.Empty, clock.GetUtcNow());

    public Task<SiteNotice> GetAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Volatile.Read(ref _current));
    }

    public Task SetAsync(string html, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Volatile.Write(ref _current, new SiteNotice(html ?? string.Empty, clock.GetUtcNow()));
        return Task.CompletedTask;
    }
}
