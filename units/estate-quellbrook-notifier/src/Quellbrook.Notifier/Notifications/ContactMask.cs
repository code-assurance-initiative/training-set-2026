namespace Quellbrook.Notifier.Notifications;

/// <summary>Masks contact details for logs and the notification log: enough to recognise, not enough to contact.</summary>
public static class ContactMask
{
    public static string Email(string email)
    {
        ArgumentNullException.ThrowIfNull(email);
        var at = email.IndexOf('@', StringComparison.Ordinal);
        return at <= 0 ? "***" : $"{email[0]}***{email[at..]}";
    }

    public static string Phone(string phone)
    {
        ArgumentNullException.ThrowIfNull(phone);
        return phone.Length < 6 ? "***" : $"{phone[..3]}***{phone[^2..]}";
    }
}
