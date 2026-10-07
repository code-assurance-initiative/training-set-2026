using Rentals.Billing.Domain.Accounts;
using Rentals.Billing.Domain.Accounts.Events;
using Rentals.SharedKernel;

namespace Rentals.Billing.Tests;

public sealed class MemberAccountTests
{
    private static readonly DateTimeOffset OpenedAt = new(2026, 5, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly MemberAccountId Id = new(Guid.Parse("6f1c2a8e-4d0b-4f57-9a43-2b8e9d1c7a55"));

    private static MemberAccount Open() => MemberAccount.Open(Id, new AccountHolder("Ada Lovelace", "ada@example.org"), "EUR", OpenedAt);

    [Fact]
    public void Charges_and_payments_make_up_the_outstanding_amount()
    {
        var account = Open();

        account.PostCharge(new Money(30m, "EUR"), "Damage", OpenedAt.AddDays(1));
        account.RecordPayment(new Money(10m, "EUR"), "PAY-1", OpenedAt.AddDays(2));

        Assert.Equal(20m, account.Outstanding);
    }

    [Fact]
    public void A_payment_reference_is_recorded_once()
    {
        var account = Open();
        account.PostCharge(new Money(30m, "EUR"), "Damage", OpenedAt);

        account.RecordPayment(new Money(10m, "EUR"), "PAY-1", OpenedAt);
        account.RecordPayment(new Money(10m, "EUR"), "PAY-1", OpenedAt);

        Assert.Equal(20m, account.Outstanding);
    }

    [Fact]
    public void A_deposit_is_held_once_per_loan_and_released()
    {
        var account = Open();
        var loan = new LoanReference(Guid.NewGuid());

        account.HoldDeposit(loan, new Money(36m, "EUR"), OpenedAt);
        account.HoldDeposit(loan, new Money(36m, "EUR"), OpenedAt);
        Assert.Equal(new Money(36m, "EUR"), Assert.Single(account.HeldDeposits).Value);

        account.ReleaseDeposit(loan, OpenedAt.AddDays(7));
        Assert.Empty(account.HeldDeposits);
    }

    [Fact]
    public void A_late_fee_is_the_daily_fee_times_the_days_late()
    {
        var account = Open();

        account.ChargeLateFee(new LoanReference(Guid.NewGuid()), 3, new Money(6m, "EUR"), OpenedAt);

        Assert.Equal(18m, account.Outstanding);
        Assert.Equal(new Money(18m, "EUR"), Assert.IsType<LateFeeCharged>(account.DomainEvents.Last()).Amount);
    }

    [Fact]
    public void Another_currency_is_refused() =>
        Assert.Throws<DomainRuleViolationException>(() => Open().PostCharge(new Money(1m, "DKK"), "x", OpenedAt));

    [Fact]
    public void Replaying_the_stream_rebuilds_the_balance_and_deposits()
    {
        var account = Open();
        var loan = new LoanReference(Guid.NewGuid());
        account.HoldDeposit(loan, new Money(36m, "EUR"), OpenedAt);
        account.PostCharge(new Money(30m, "EUR"), "Damage", OpenedAt);
        account.RecordPayment(new Money(5m, "EUR"), "PAY-1", OpenedAt);

        var replayed = MemberAccount.FromHistory(Id, account.DomainEvents);

        Assert.Equal(account.Outstanding, replayed.Outstanding);
        Assert.Equal(account.HeldDeposits, replayed.HeldDeposits);
        Assert.Equal(account.DomainEvents.Count, replayed.Version);
    }

    [Fact]
    public void A_charge_reason_is_remembered()
    {
        var account = Open();

        account.PostCharge(new Money(2m, "EUR"), "Extension of loan 1 to 2026-05-10", OpenedAt);

        Assert.True(account.HasChargeWithReason("Extension of loan 1 to 2026-05-10"));
        Assert.False(account.HasChargeWithReason("Extension of loan 1 to 2026-05-11"));
    }
}
