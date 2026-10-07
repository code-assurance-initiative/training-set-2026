using Rentals.Billing.Domain.EventSourcing;

namespace Rentals.Billing.Domain.Accounts;

/// <summary>Loads an account by replaying its stream and saves the events it emitted since.</summary>
public interface IMemberAccountRepository
{
    Task<bool> ExistsAsync(MemberAccountId id, CancellationToken cancellationToken);

    Task<MemberAccount?> LoadAsync(MemberAccountId id, CancellationToken cancellationToken);

    Task SaveAsync(MemberAccount account, InboxReceipt? receipt, CancellationToken cancellationToken);
}
