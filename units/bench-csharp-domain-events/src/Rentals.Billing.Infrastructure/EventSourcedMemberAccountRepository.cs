using Rentals.Billing.Domain.Accounts;
using Rentals.Billing.Domain.EventSourcing;

namespace Rentals.Billing.Infrastructure;

internal sealed class EventSourcedMemberAccountRepository(IEventStore store) : IMemberAccountRepository
{
    public Task<bool> ExistsAsync(MemberAccountId id, CancellationToken cancellationToken) =>
        store.ExistsAsync(id.StreamId, cancellationToken);

    public async Task<MemberAccount?> LoadAsync(MemberAccountId id, CancellationToken cancellationToken)
    {
        var history = await store.ReadStreamAsync(id.StreamId, cancellationToken).ConfigureAwait(false);
        return history.Count == 0 ? null : MemberAccount.FromHistory(id, history);
    }

    public async Task SaveAsync(MemberAccount account, InboxReceipt? receipt, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(account);
        if (account.DomainEvents.Count == 0 && receipt is null)
        {
            return;
        }

        await store.AppendAsync(account.Id.StreamId, account.Version, [.. account.DomainEvents], receipt, cancellationToken)
            .ConfigureAwait(false);
        account.MarkCommitted();
    }
}
