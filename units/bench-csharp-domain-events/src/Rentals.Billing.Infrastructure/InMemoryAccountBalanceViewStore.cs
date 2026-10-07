using System.Collections.Concurrent;
using Rentals.Billing.Application.Projections;

namespace Rentals.Billing.Infrastructure;

internal sealed class InMemoryAccountBalanceViewStore : IAccountBalanceViewStore
{
    private readonly ConcurrentDictionary<Guid, AccountBalanceView> _views = new();

    public long Checkpoint { get; set; }

    public AccountBalanceView? Find(Guid accountId) => _views.TryGetValue(accountId, out var view) ? view : null;

    public void Upsert(AccountBalanceView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        _views[view.AccountId] = view;
    }
}
