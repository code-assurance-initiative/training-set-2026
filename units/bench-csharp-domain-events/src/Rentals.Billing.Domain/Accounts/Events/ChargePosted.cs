using Rentals.SharedKernel;

namespace Rentals.Billing.Domain.Accounts.Events;

public sealed record ChargePosted(
    MemberAccountId AccountId,
    ChargeId ChargeId,
    Money Amount,
    string Reason,
    DateTimeOffset OccurredAt) : IDomainEvent;
