using Rentals.SharedKernel;

namespace Rentals.Billing.Domain.Accounts.Events;

public sealed class DepositReleased : IDomainEvent
{
    public required MemberAccountId AccountId { get; init; }

    public required LoanReference Loan { get; init; }

    public required Money Amount { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }
}
