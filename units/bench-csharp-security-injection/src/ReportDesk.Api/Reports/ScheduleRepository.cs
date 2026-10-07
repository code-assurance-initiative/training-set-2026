using Microsoft.EntityFrameworkCore;

namespace ReportDesk.Api.Reports;

public sealed class ScheduleRepository(ReportsDbContext db) : IScheduleRepository
{
    public async Task<IReadOnlyList<ReportSchedule>> DueForOwnerAsync(
        string owner, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return await db.Schedules
            .FromSqlInterpolated($"SELECT * FROM report_schedules WHERE owner = {owner} AND next_run_at <= {now}")
            .AsNoTracking()
            .OrderBy(schedule => schedule.NextRunAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
