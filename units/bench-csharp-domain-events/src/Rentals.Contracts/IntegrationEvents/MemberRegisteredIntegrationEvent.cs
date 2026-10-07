using Rentals.Messaging;

namespace Rentals.Contracts.IntegrationEvents;

/// <summary>A member joined the library. Billing opens an account for them.</summary>
public sealed record MemberRegisteredIntegrationEvent(
    Guid MemberId,
    string FullName,
    string Email,
    DateTimeOffset OccurredAt) : IIntegrationEvent;
