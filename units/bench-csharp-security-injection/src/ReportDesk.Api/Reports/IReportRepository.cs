namespace ReportDesk.Api.Reports;

public interface IReportRepository
{
    Task<ReportDefinition?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReportDefinition>> ForOwnerAsync(string owner, CancellationToken cancellationToken);

    Task SaveLayoutAsync(Guid id, string layoutJson, CancellationToken cancellationToken);
}
