namespace ReportDesk.Api.Shares;

public interface IShareStore
{
    Task AddAsync(ShareLink link, CancellationToken cancellationToken);

    Task<ShareLink?> FindAsync(string token, CancellationToken cancellationToken);

    Task<int> PurgeExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken);
}
