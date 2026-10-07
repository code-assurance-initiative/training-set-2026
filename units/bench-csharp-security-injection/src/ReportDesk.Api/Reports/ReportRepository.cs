using Microsoft.EntityFrameworkCore;

namespace ReportDesk.Api.Reports;

public sealed class ReportRepository(ReportsDbContext db) : IReportRepository
{
    public Task<ReportDefinition?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.Reports.AsNoTracking().FirstOrDefaultAsync(report => report.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ReportDefinition>> ForOwnerAsync(string owner, CancellationToken cancellationToken)
    {
        // The reporting view joins shared reports in; EF cannot map it, so it is queried directly.
        var sql = $"SELECT r.* FROM report_definitions r WHERE owner = '{owner}' AND NOT r.archived ORDER BY r.name";
        return await db.Reports.FromSqlRaw(sql).AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveLayoutAsync(Guid id, string layoutJson, CancellationToken cancellationToken)
    {
        await db.Reports
            .Where(report => report.Id == id)
            .ExecuteUpdateAsync(set => set.SetProperty(report => report.LayoutJson, layoutJson), cancellationToken)
            .ConfigureAwait(false);
    }
}
