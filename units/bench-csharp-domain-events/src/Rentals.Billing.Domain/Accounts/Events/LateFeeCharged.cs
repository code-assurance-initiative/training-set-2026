using Rentals.SharedKernel;

namespace Rentals.Billing.Domain.Accounts.Events;

/// <summary>
/// A late fee: the daily fee for each started day past the due date. Schema version 2 records the daily fee (version 1
/// recorded only the total); stored version-1 events are upcast when read.
/// </summary>
public sealed record LateFeeCharged(
    MemberAccountId AccountId,
    LoanReference Loan,
    int DaysLate,
    Money DailyFee,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public Money Amount => DailyFee.Multiply(DaysLate);
}
