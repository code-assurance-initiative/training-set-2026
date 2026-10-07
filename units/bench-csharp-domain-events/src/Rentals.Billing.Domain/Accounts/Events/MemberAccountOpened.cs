using Rentals.SharedKernel;

namespace Rentals.Billing.Domain.Accounts.Events;

public sealed record MemberAccountOpened(
    MemberAccountId AccountId,
    string HolderFullName,
    string HolderEmail,
    string Currency,
    DateTimeOffset OccurredAt) : IDomainEvent;
