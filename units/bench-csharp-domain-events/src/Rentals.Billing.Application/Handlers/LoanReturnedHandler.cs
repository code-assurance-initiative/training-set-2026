using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rentals.Billing.Domain.Accounts;
using Rentals.Billing.Domain.EventSourcing;
using Rentals.Contracts.IntegrationEvents;
using Rentals.Messaging;
using Rentals.SharedKernel;

namespace Rentals.Billing.Application.Handlers;

/// <summary>Releases the loan's deposit and charges a late fee for every started day past the due date.</summary>
public sealed partial class LoanReturnedHandler(
    IMemberAccountRepository accounts, IInboxStore inbox, IOptions<BillingOptions> options, ILogger<LoanReturnedHandler> logger)
    : IIntegrationEventHandler<LoanReturnedIntegrationEvent>
{
    private const string Consumer = "billing.loan-returned";

    public async Task HandleAsync(LoanReturnedIntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        ArgumentNullException.ThrowIfNull(context);
        if (await inbox.HasProcessedAsync(Consumer, context.MessageId, cancellationToken).ConfigureAwait(false))
        {
            LogRedelivery(logger, context.MessageId);
            return;
        }

        var account = await accounts.LoadRequiredAsync(integrationEvent.MemberId, cancellationToken).ConfigureAwait(false);
        var loan = new LoanReference(integrationEvent.LoanId);
        account.ReleaseDeposit(loan, integrationEvent.ReturnedAt);
        if (integrationEvent.ReturnedAt > integrationEvent.DueAt)
        {
            var daysLate = (int)Math.Ceiling((integrationEvent.ReturnedAt - integrationEvent.DueAt).TotalDays);
            account.ChargeLateFee(loan, daysLate, LateFee(integrationEvent), integrationEvent.ReturnedAt);
            LogLateFee(logger, daysLate, integrationEvent.LoanId);
        }

        await accounts.SaveAsync(account, new InboxReceipt(Consumer, context.MessageId), cancellationToken).ConfigureAwait(false);
    }

    private Money LateFee(LoanReturnedIntegrationEvent integrationEvent) =>
        new(integrationEvent.DailyRate.Amount * options.Value.LateFeeRateMultiple, integrationEvent.DailyRate.Currency);

    [LoggerMessage(Level = LogLevel.Information, Message = "LoanReturned message {MessageId} was already processed; skipped")]
    private static partial void LogRedelivery(ILogger logger, Guid messageId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Charged a late fee for {DaysLate} day(s) on loan {LoanId}")]
    private static partial void LogLateFee(ILogger logger, int daysLate, Guid loanId);
}
