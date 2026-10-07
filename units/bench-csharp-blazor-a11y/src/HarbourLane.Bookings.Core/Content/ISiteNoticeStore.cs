namespace HarbourLane.Bookings.Content;

public interface ISiteNoticeStore
{
    Task<SiteNotice> GetAsync(CancellationToken cancellationToken);

    Task SetAsync(string html, CancellationToken cancellationToken);
}
