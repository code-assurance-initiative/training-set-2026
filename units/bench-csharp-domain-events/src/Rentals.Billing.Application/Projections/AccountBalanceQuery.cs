using Rentals.Lending.Domain.Members;
using Rentals.Messaging;

namespace Rentals.Billing.Application.Projections;

public sealed record AccountBalanceQuery(MemberId MemberId);

public sealed class AccountBalanceQueryHandler(IAccountBalanceViewStore views) : IQueryHandler<AccountBalanceQuery, AccountBalanceView?>
{
    public Task<AccountBalanceView?> HandleAsync(AccountBalanceQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return Task.FromResult(views.Find(query.MemberId.Value));
    }
}
