namespace Rentals.Billing.Domain.EventSourcing;

/// <summary>The stream moved on since it was read; the command must be retried on the new state.</summary>
public sealed class ConcurrencyException : Exception
{
    public ConcurrencyException()
    {
    }

    public ConcurrencyException(string message)
        : base(message)
    {
    }

    public ConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
