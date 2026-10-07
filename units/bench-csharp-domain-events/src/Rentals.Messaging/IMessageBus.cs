namespace Rentals.Messaging;

/// <summary>The transport seam. Publish fans an event out to every subscriber; Send delivers a command to one endpoint.</summary>
public interface IMessageBus
{
    Task PublishAsync(IIntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken);

    Task SendAsync(object command, string destination, CancellationToken cancellationToken);
}
