namespace Rentals.Lending.Infrastructure.Messaging;

/// <summary>An integration event committed with the business change that produced it, waiting to be published.</summary>
internal sealed class OutboxMessage
{
    public OutboxMessage(Guid id, string type, string payload, DateTimeOffset occurredAt)
    {
        Id = id;
        Type = type;
        Payload = payload;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; }

    public string Type { get; private set; }

    public string Payload { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public int Attempts { get; private set; }

    public string? LastError { get; private set; }

    public void RecordFailure(string error)
    {
        Attempts++;
        LastError = error.Length > 2000 ? error[..2000] : error;
    }
}
