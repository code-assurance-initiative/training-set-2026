using Rentals.Messaging;

namespace Rentals.Contracts.IntegrationEvents;

/// <summary>A unit was lent out. Billing holds a deposit against it.</summary>
public sealed record LoanOpenedIntegrationEvent(
    Guid LoanId,
    Guid MemberId,
    Guid EquipmentId,
    DateTimeOffset DueAt,
    DateTimeOffset OccurredAt) : IIntegrationEvent;
