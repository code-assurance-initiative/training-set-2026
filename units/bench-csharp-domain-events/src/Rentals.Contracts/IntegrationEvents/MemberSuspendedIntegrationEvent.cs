using Rentals.Messaging;

namespace Rentals.Contracts.IntegrationEvents;

/// <summary>A member may no longer borrow until reinstated.</summary>
public sealed record MemberSuspendedIntegrationEvent(Guid MemberId, string Reason, DateTimeOffset OccurredAt) : IIntegrationEvent;
