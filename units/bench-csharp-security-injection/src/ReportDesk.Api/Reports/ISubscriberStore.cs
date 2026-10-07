namespace ReportDesk.Api.Reports;

public interface ISubscriberStore
{
    Task<Subscriber?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task SetEmailEnabledAsync(Guid id, bool enabled, CancellationToken cancellationToken);
}
