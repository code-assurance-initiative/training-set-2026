using Rentals.Billing.Domain.Accounts.Events;
using Rentals.Billing.Domain.EventSourcing;
using Rentals.SharedKernel;

namespace Rentals.Billing.Domain.Accounts;

/// <summary>
/// What a member owes the library: posted charges and late fees, less payments; deposits held against open loans are
/// tracked separately. Event-sourced: the state below is rebuilt from the account's stream.
/// </summary>
public sealed class MemberAccount : EventSourcedAggregate<MemberAccountId>
{
    private readonly Dictionary<LoanReference, Money> _heldDeposits = [];
    private readonly HashSet<string> _chargeReasons = new(StringComparer.Ordinal);
    private readonly HashSet<string> _paymentReferences = new(StringComparer.Ordinal);

    private MemberAccount(MemberAccountId id)
        : base(id)
    {
    }

    public AccountHolder? Holder { get; private set; }

    public string Currency { get; private set; } = "EUR";

    public Money Balance { get; private set; } = Money.Zero("EUR");

    public Money PaidToDate { get; private set; } = Money.Zero("EUR");

    public DateTimeOffset LastActivityAt { get; private set; }

    public IReadOnlyDictionary<LoanReference, Money> HeldDeposits => _heldDeposits.AsReadOnly();

    /// <summary>What the member still owes: charges and fees less payments.</summary>
    public decimal Outstanding => Balance.Amount - PaidToDate.Amount;

    public static MemberAccount Open(MemberAccountId id, AccountHolder holder, string currency, DateTimeOffset openedAt)
    {
        ArgumentNullException.ThrowIfNull(holder);
        var account = new MemberAccount(id);
        account.Emit(new MemberAccountOpened(id, holder.FullName, holder.Email, Money.Zero(currency).Currency, openedAt));
        return account;
    }

    public static MemberAccount FromHistory(MemberAccountId id, IEnumerable<IDomainEvent> history)
    {
        var account = new MemberAccount(id);
        account.LoadFrom(history);
        return account;
    }

    public bool HasChargeWithReason(string reason) => _chargeReasons.Contains(reason);

    public ChargeId PostCharge(Money amount, string reason, DateTimeOffset postedAt)
    {
        ArgumentNullException.ThrowIfNull(amount);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        EnsureCurrency(amount);
        var chargeId = new ChargeId(Guid.NewGuid());
        Emit(new ChargePosted(Id, chargeId, amount, reason, postedAt));
        return chargeId;
    }

    public void HoldDeposit(LoanReference loan, Money amount, DateTimeOffset heldAt)
    {
        ArgumentNullException.ThrowIfNull(amount);
        EnsureCurrency(amount);
        if (_heldDeposits.ContainsKey(loan))
        {
            return;
        }

        Emit(new DepositHeld(Id, loan, amount, heldAt));
    }

    public void ReleaseDeposit(LoanReference loan, DateTimeOffset releasedAt)
    {
        if (!_heldDeposits.TryGetValue(loan, out var amount))
        {
            return;
        }

        Emit(new DepositReleased { AccountId = Id, Loan = loan, Amount = amount, OccurredAt = releasedAt });
    }

    public void ChargeLateFee(LoanReference loan, int daysLate, Money dailyFee, DateTimeOffset chargedAt)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(daysLate);
        ArgumentNullException.ThrowIfNull(dailyFee);
        EnsureCurrency(dailyFee);
        Emit(new LateFeeCharged(Id, loan, daysLate, dailyFee, chargedAt));
    }

    public void RecordPayment(Money amount, string paymentReference, DateTimeOffset receivedAt)
    {
        ArgumentNullException.ThrowIfNull(amount);
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentReference);
        EnsureCurrency(amount);
        if (_paymentReferences.Contains(paymentReference))
        {
            return;
        }

        Emit(new PaymentReceived { AccountId = Id, Amount = amount, PaymentReference = paymentReference, OccurredAt = receivedAt });
    }

    protected override void Apply(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case MemberAccountOpened e: Apply(e); break;
            case ChargePosted e: Apply(e); break;
            case DepositHeld e: Apply(e); break;
            case DepositReleased e: Apply(e); break;
            case LateFeeCharged e: Apply(e); break;
            case PaymentReceived e: Apply(e); break;
            default: throw new InvalidOperationException($"{domainEvent.GetType().Name} does not belong to a member account.");
        }
    }

    private void Apply(MemberAccountOpened e)
    {
        Holder = new AccountHolder(e.HolderFullName, e.HolderEmail);
        Currency = e.Currency;
        Balance = Money.Zero(e.Currency);
        PaidToDate = Money.Zero(e.Currency);
        LastActivityAt = e.OccurredAt;
    }

    private void Apply(ChargePosted e)
    {
        Balance = Balance.Add(e.Amount);
        _chargeReasons.Add(e.Reason);
        LastActivityAt = e.OccurredAt;
    }

    private void Apply(DepositHeld e)
    {
        _heldDeposits[e.LoanId] = e.Amount;
        LastActivityAt = e.OccurredAt;
    }

    private void Apply(DepositReleased e)
    {
        _heldDeposits.Remove(e.Loan);
        LastActivityAt = e.OccurredAt;
    }

    private void Apply(LateFeeCharged e)
    {
        Balance = Balance.Add(e.Amount);
        LastActivityAt = DateTimeOffset.UtcNow;
    }

    private void Apply(PaymentReceived e)
    {
        PaidToDate = PaidToDate.Add(e.Amount);
        _paymentReferences.Add(e.PaymentReference);
        LastActivityAt = e.OccurredAt;
    }

    private void EnsureCurrency(Money amount)
    {
        if (!string.Equals(amount.Currency, Currency, StringComparison.Ordinal))
        {
            throw new DomainRuleViolationException($"This account is kept in {Currency}, not {amount.Currency}.");
        }
    }
}
