namespace Quellbrook.Notifier.Channels;

public sealed record SmsMessage(Guid OrderId, string To, string Text);

/// <summary>Sends one SMS; throws <see cref="ProviderException"/> when the gateway refuses it.</summary>
public interface ISmsSender
{
    Task SendAsync(SmsMessage message, CancellationToken cancellationToken);
}
