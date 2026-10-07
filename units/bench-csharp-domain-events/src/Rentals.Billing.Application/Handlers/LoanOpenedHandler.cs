using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rentals.Billing.Domain.Accounts;
using Rentals.Billing.Domain.EventSourcing;
using Rentals.Contracts.IntegrationEvents;
using Rentals.Messaging;
using Rentals.SharedKernel;

namespace Rentals.Billing.Application.Handlers;

/// <summary>Holds a deposit against a new loan: a share of the item's replacement value, asked of the catalogue.</summary>
public sealed partial class LoanOpenedHandler(
    HttpClient catalogue,
    IMemberAccountRepository accounts,
    IInboxStore inbox,
    IOptions<BillingOptions> options,
    ILogger<LoanOpenedHandler> logger) : IIntegrationEventHandler<LoanOpenedIntegrationEvent>
{
    private const string Consumer = "billing.loan-opened";

    public async Task HandleAsync(LoanOpenedIntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        ArgumentNullException.ThrowIfNull(context);
        if (await inbox.HasProcessedAsync(Consumer, context.MessageId, cancellationToken).ConfigureAwait(false))
        {
            LogRedelivery(logger, context.MessageId);
            return;
        }

        var valuation = await catalogue
            .GetFromJsonAsync<EquipmentValuation>($"equipment/{integrationEvent.EquipmentId:N}/valuation", cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"The catalogue has no valuation for {integrationEvent.EquipmentId}.");
        var account = await accounts.LoadRequiredAsync(integrationEvent.MemberId, cancellationToken).ConfigureAwait(false);
        var deposit = new Money(valuation.ReplacementValue * options.Value.DepositShare, valuation.Currency);
        account.HoldDeposit(new LoanReference(integrationEvent.LoanId), deposit, integrationEvent.OccurredAt);
        await accounts.SaveAsync(account, new InboxReceipt(Consumer, context.MessageId), cancellationToken).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "LoanOpened message {MessageId} was already processed; skipped")]
    private static partial void LogRedelivery(ILogger logger, Guid messageId);
}
