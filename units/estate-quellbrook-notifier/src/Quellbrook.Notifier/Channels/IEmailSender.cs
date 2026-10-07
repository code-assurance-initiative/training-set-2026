namespace Quellbrook.Notifier.Channels;

public sealed record EmailMessage(Guid OrderId, string To, string ToName, string Subject, string Body);

/// <summary>Sends one plain-text e-mail; throws <see cref="ProviderException"/> when the provider refuses it.</summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

/// <summary>A notification provider refused or failed a request.</summary>
public sealed class ProviderException(string message) : Exception(message);
