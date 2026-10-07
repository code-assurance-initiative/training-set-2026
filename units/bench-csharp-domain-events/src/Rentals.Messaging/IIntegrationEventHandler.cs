namespace Rentals.Messaging;

/// <summary>Consumes one integration event type. Delivery is at least once: handlers must tolerate redelivery.</summary>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, MessageContext context, CancellationToken cancellationToken);
}
