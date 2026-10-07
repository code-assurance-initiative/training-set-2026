using Rentals.Billing.Domain.Accounts;
using Rentals.Contracts.IntegrationEvents;
using Rentals.Messaging;
using Microsoft.Extensions.Options;

namespace Rentals.Billing.Application.Handlers;

/// <summary>Opens a billing account for a new member, unless one is already open.</summary>
public sealed class MemberRegisteredHandler(IMemberAccountRepository accounts, IOptions<BillingOptions> options)
    : IIntegrationEventHandler<MemberRegisteredIntegrationEvent>
{
    public async Task HandleAsync(
        MemberRegisteredIntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        var id = new MemberAccountId(integrationEvent.MemberId);
        if (await accounts.ExistsAsync(id, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var holder = new AccountHolder(integrationEvent.FullName, integrationEvent.Email);
        var account = MemberAccount.Open(id, holder, options.Value.Currency, integrationEvent.OccurredAt);
        await accounts.SaveAsync(account, receipt: null, cancellationToken).ConfigureAwait(false);
    }
}
