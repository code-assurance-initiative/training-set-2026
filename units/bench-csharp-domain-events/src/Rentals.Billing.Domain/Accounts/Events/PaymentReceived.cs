using Rentals.SharedKernel;

namespace Rentals.Billing.Domain.Accounts.Events;

public sealed class PaymentReceived : IDomainEvent
{
    public MemberAccountId AccountId { get; set; }

    public Money Amount { get; set; } = Money.Zero("EUR");

    public string PaymentReference { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }
}
