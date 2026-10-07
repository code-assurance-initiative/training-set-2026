namespace Rentals.Billing.Application.Projections;

public interface IAccountBalanceViewStore
{
    AccountBalanceView? Find(Guid accountId);

    void Upsert(AccountBalanceView view);

    /// <summary>The log position of the last event projected; the runner resumes after it.</summary>
    long Checkpoint { get; set; }
}
