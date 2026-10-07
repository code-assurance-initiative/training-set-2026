namespace ReportDesk.Api.Reports;

/// <summary>A person who receives a scheduled report by e-mail.</summary>
public sealed class Subscriber
{
    public Guid Id { get; set; }

    public Guid ReportId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string EmailAddress { get; set; } = string.Empty;

    public bool EmailEnabled { get; set; } = true;
}
