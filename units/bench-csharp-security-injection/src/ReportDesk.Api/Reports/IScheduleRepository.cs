namespace ReportDesk.Api.Reports;

public interface IScheduleRepository
{
    Task<IReadOnlyList<ReportSchedule>> DueForOwnerAsync(string owner, DateTimeOffset now, CancellationToken cancellationToken);
}
