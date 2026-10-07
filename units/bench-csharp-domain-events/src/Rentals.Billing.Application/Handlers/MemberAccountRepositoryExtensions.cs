using Rentals.Billing.Domain.Accounts;

namespace Rentals.Billing.Application.Handlers;

internal static class MemberAccountRepositoryExtensions
{
    /// <summary>Loads the account of a member Lending has told us about; its absence means a lost registration.</summary>
    public static async Task<MemberAccount> LoadRequiredAsync(
        this IMemberAccountRepository accounts, Guid memberId, CancellationToken cancellationToken) =>
        await accounts.LoadAsync(new MemberAccountId(memberId), cancellationToken).ConfigureAwait(false)
        ?? throw new InvalidOperationException($"No billing account for member {memberId}.");
}
