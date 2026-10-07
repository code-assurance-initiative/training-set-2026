using Rentals.Billing.Domain.Accounts;
using Rentals.Billing.Domain.Accounts.Events;
using Rentals.Billing.Domain.EventSourcing;

namespace Rentals.Billing.Application.Projections;

/// <summary>Folds the account streams into <see cref="AccountBalanceView"/> rows. Rebuilding from position 0 must
/// produce the same rows.</summary>
public sealed class AccountBalanceProjection(IAccountBalanceViewStore views)
{
    public void Project(StoredEvent stored)
    {
        ArgumentNullException.ThrowIfNull(stored);
        switch (stored.Event)
        {
            case MemberAccountOpened e: When(e); break;
            case ChargePosted e: When(e); break;
            case LateFeeCharged e: When(e); break;
            case DepositHeld e: When(e); break;
            case DepositReleased e: When(e); break;
            case PaymentReceived e: When(e); break;
        }

        views.Checkpoint = stored.Position;
    }

    public void When(MemberAccountOpened e) =>
        views.Upsert(new AccountBalanceView { AccountId = e.AccountId.Value, Currency = e.Currency });

    public void When(ChargePosted e) => Update(e.AccountId, v => v.Charged += e.Amount.Amount);

    public void When(LateFeeCharged e) => Update(e.AccountId, v => v.Charged += e.Amount.Amount);

    public void When(DepositHeld e) => Update(e.AccountId, v =>
    {
        v.DepositsHeld += e.Amount.Amount;
        v.OpenLoans++;
    });

    public void When(DepositReleased e) => Update(e.AccountId, v =>
    {
        v.DepositsHeld -= e.Amount.Amount;
        v.OpenLoans--;
    });

    public void When(PaymentReceived e) => Update(e.AccountId, v =>
    {
        v.Paid += e.Amount.Amount;
        v.LastPaymentAmount = e.Amount.Amount;
        v.LastPaymentAt = DateTimeOffset.UtcNow;
    });

    private void Update(MemberAccountId accountId, Action<AccountBalanceView> change)
    {
        var view = views.Find(accountId.Value)
            ?? throw new InvalidOperationException($"No balance view for account {accountId}; the opening event was not projected.");
        change(view);
        views.Upsert(view);
    }
}
