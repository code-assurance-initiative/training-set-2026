namespace FleetOps.Infrastructure.Email;

public enum EmailPriority
{
    Normal,
    High,
}

public sealed record EmailAttachment(string FileName, string ContentType, ReadOnlyMemory<byte> Content);

public sealed record EmailMessage(
    EmailAddress To,
    string Subject,
    string Body,
    EmailPriority Priority,
    IReadOnlyList<EmailAttachment> Attachments);

public static class EmailTemplateNames
{
    public const string MaintenanceReminder = "maintenance-reminder";
    public const string WorkOrderApproved = "work-order-approved";
}
