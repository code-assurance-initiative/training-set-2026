namespace Quellbrook.Notifier.Messaging;

/// <summary>
/// The notifier's copy of <c>orders.order-placed.v1</c>: only the consignee's name and contact details
/// (contracts/consumed/orders/order-placed.v1.schema.json). Unknown fields are ignored.
/// </summary>
public sealed record OrderPlacedMessage(Guid OrderId, OrderPlacedMessage.ConsigneePart Consignee)
{
    public const string EventType = "orders.order-placed.v1";

    public sealed record ConsigneePart(string Name, ContactPart Contact);

    public sealed record ContactPart(string? Email, string? Phone);
}

/// <summary>The notifier's copy of <c>dispatch.consignment-out-for-delivery.v1</c>.</summary>
public sealed record ConsignmentOutForDeliveryMessage(Guid ConsignmentId, Guid OrderId, DateTimeOffset OutForDeliveryAt)
{
    public const string EventType = "dispatch.consignment-out-for-delivery.v1";
}

/// <summary>The notifier's copy of <c>dispatch.consignment-delivered.v1</c>.</summary>
public sealed record ConsignmentDeliveredMessage(Guid ConsignmentId, Guid OrderId, string Proof, DateTimeOffset DeliveredAt)
{
    public const string EventType = "dispatch.consignment-delivered.v1";
}
