namespace Rentals.Messaging;

/// <summary>A fact published by one bounded context for others. Carries primitives and shared-kernel types only.</summary>
public interface IIntegrationEvent
{
    DateTimeOffset OccurredAt { get; }
}
