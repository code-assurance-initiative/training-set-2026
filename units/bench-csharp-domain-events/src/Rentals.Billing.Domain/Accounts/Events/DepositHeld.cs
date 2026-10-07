using Rentals.SharedKernel;

namespace Rentals.Billing.Domain.Accounts.Events;

public sealed record DepositHeld(MemberAccountId AccountId, LoanReference LoanId, Money Amount, DateTimeOffset OccurredAt) : IDomainEvent;
