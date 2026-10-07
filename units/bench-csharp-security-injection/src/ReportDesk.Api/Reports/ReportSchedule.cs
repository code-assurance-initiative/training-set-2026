namespace ReportDesk.Api.Reports;

public sealed class ReportSchedule
{
    public Guid Id { get; set; }

    public Guid ReportId { get; set; }

    public string Owner { get; set; } = string.Empty;

    public string Cron { get; set; } = string.Empty;

    public DateTimeOffset NextRunAt { get; set; }
}
