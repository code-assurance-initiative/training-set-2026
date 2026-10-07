using Rentals.Messaging;
using Rentals.SharedKernel;

namespace Rentals.Contracts.IntegrationEvents;

/// <summary>A unit came back. Billing releases the deposit and charges any late fee.</summary>
public sealed record LoanReturnedIntegrationEvent(
    Guid LoanId,
    Guid MemberId,
    DateTimeOffset DueAt,
    DateTimeOffset ReturnedAt,
    Money DailyRate,
    DateTimeOffset OccurredAt) : IIntegrationEvent;
