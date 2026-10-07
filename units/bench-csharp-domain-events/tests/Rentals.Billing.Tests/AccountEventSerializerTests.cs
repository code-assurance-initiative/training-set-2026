using Rentals.Billing.Domain.Accounts;
using Rentals.Billing.Domain.Accounts.Events;
using Rentals.Billing.Infrastructure;
using Rentals.SharedKernel;

namespace Rentals.Billing.Tests;

public sealed class AccountEventSerializerTests
{
    private static readonly MemberAccountId Account = new(Guid.Parse("5c2e9a41-7f03-4d6b-8e1a-0b94c3d7e215"));
    private static readonly LoanReference Loan = new(Guid.Parse("b81d4f2a-96c3-4e07-a5d8-3f1e6c90b742"));
    private static readonly DateTimeOffset At = new(2026, 3, 14, 9, 30, 0, TimeSpan.Zero);

    public static TheoryData<IDomainEvent> Events() =>
    [
        new MemberAccountOpened(Account, "Ada Example", "ada@example.org", "EUR", At),
        new ChargePosted(Account, new ChargeId(Guid.Parse("0f6b2d83-4a1c-4b9e-8d27-c5e3a1f09b64")), new Money(12.5m, "EUR"), "Damage", At),
        new DepositHeld(Account, Loan, new Money(40m, "EUR"), At),
        new DepositReleased { AccountId = Account, Loan = Loan, Amount = new Money(40m, "EUR"), OccurredAt = At },
        new LateFeeCharged(Account, Loan, 3, new Money(6m, "EUR"), At),
    ];

    [Theory]
    [MemberData(nameof(Events))]
    public void Every_account_event_reads_back_as_it_was_written(IDomainEvent domainEvent)
    {
        var stored = AccountEventSerializer.Serialize(domainEvent);

        Assert.Equal(domainEvent, AccountEventSerializer.Deserialize(stored), new JsonShapeComparer());
    }

    [Fact]
    public void A_payment_reads_back_with_its_amount_and_reference()
    {
        var payment = new PaymentReceived { AccountId = Account, Amount = new Money(9m, "EUR"), PaymentReference = "PAY-1", OccurredAt = At };

        var read = Assert.IsType<PaymentReceived>(AccountEventSerializer.Deserialize(AccountEventSerializer.Serialize(payment)));

        Assert.Equal((Account, new Money(9m, "EUR"), "PAY-1", At), (read.AccountId, read.Amount, read.PaymentReference, read.OccurredAt));
    }

    [Fact]
    public void A_version_1_late_fee_is_upcast_to_its_daily_fee()
    {
        const string v1 = """
            {"accountId":{"value":"5c2e9a41-7f03-4d6b-8e1a-0b94c3d7e215"},"loan":{"value":"b81d4f2a-96c3-4e07-a5d8-3f1e6c90b742"},
             "daysLate":3,"amount":{"amount":18.00,"currency":"EUR"},"occurredAt":"2026-03-14T09:30:00+00:00"}
            """;

        var read = Assert.IsType<LateFeeCharged>(AccountEventSerializer.Deserialize(new SerializedEvent("late-fee-charged", 1, v1)));

        Assert.Equal(new LateFeeCharged(Account, Loan, 3, new Money(6m, "EUR"), At), read);
        Assert.Equal(new Money(18m, "EUR"), read.Amount);
    }

    [Fact]
    public void An_unknown_stored_type_is_refused()
    {
        Assert.Throws<InvalidOperationException>(() =>
            AccountEventSerializer.Deserialize(new SerializedEvent("loyalty-points-earned", 1, "{}")));
    }

    /// <summary>Records compare by value; the one class-shaped event is compared through its stored form.</summary>
    private sealed class JsonShapeComparer : IEqualityComparer<IDomainEvent>
    {
        public bool Equals(IDomainEvent? x, IDomainEvent? y) =>
            x is not null && y is not null && x.GetType() == y.GetType()
            && AccountEventSerializer.Serialize(x) == AccountEventSerializer.Serialize(y);

        public int GetHashCode(IDomainEvent obj) => obj.GetType().GetHashCode();
    }
}
