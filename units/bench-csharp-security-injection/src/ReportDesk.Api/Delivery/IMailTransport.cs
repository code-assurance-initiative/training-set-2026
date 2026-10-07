namespace ReportDesk.Api.Delivery;

public sealed record MailMessageRequest(string To, string Subject, string Body, string AttachmentName, byte[] Attachment);

public interface IMailTransport
{
    Task SendAsync(MailMessageRequest message, CancellationToken cancellationToken);
}
