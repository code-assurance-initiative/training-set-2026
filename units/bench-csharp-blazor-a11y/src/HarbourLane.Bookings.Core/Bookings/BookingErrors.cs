namespace HarbourLane.Bookings.Bookings;

/// <summary>Validation messages keyed by the field they belong to.</summary>
public sealed class BookingErrors
{
    private readonly Dictionary<BookingField, string> _messages = [];

    public bool IsEmpty => _messages.Count == 0;

    public IReadOnlyDictionary<BookingField, string> Messages => _messages;

    public string? For(BookingField field) => _messages.GetValueOrDefault(field);

    internal void Add(BookingField field, string message) => _messages.TryAdd(field, message);
}
