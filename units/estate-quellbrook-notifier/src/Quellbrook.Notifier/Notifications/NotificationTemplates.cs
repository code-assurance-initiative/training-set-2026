namespace Quellbrook.Notifier.Notifications;

/// <summary>The plain-text messages the consignee receives. Order references are the first eight characters of the id.</summary>
public static class NotificationTemplates
{
    public static string Reference(Guid orderId) => orderId.ToString("N")[..8].ToUpperInvariant();

    /// <summary>The e-mail for a kind of notification, or null when the kind has none.</summary>
    public static (string Subject, string Body)? Email(NotificationKind kind, string name, Guid orderId)
    {
        var reference = Reference(orderId);
        return kind switch
        {
            NotificationKind.OrderConfirmed => (
                $"Your Quellbrook delivery {reference} is booked",
                $"Hello {name},\n\nA parcel delivery to you has been booked with Quellbrook Freight (reference {reference}). " +
                "We will tell you when it is on its way.\n\nQuellbrook Freight"),
            NotificationKind.OutForDelivery => (
                $"Your Quellbrook delivery {reference} arrives today",
                $"Hello {name},\n\nYour parcels (reference {reference}) left our depot this morning and will be delivered " +
                "today.\n\nQuellbrook Freight"),
            NotificationKind.Delivered => (
                $"Your Quellbrook delivery {reference} has been delivered",
                $"Hello {name},\n\nYour parcels (reference {reference}) have been delivered.\n\nQuellbrook Freight"),
            _ => null,
        };
    }

    /// <summary>The SMS for a kind of notification, or null when the kind has none (only delivery-day messages).</summary>
    public static string? Sms(NotificationKind kind, Guid orderId) => kind switch
    {
        NotificationKind.OutForDelivery => $"Quellbrook: your parcels {Reference(orderId)} are out for delivery and arrive today.",
        _ => null,
    };
}
