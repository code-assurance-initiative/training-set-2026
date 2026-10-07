using Microsoft.Extensions.Logging;
using Rentals.Billing.Domain.Accounts;
using Rentals.Contracts.IntegrationEvents;
using Rentals.Lending.Domain.Catalogue;
using Rentals.Messaging;
using Rentals.SharedKernel;

namespace Rentals.Billing.Application.Handlers;

/// <summary>Charges the member for a unit returned damaged (half its replacement value) or lost (all of it).</summary>
public sealed partial class EquipmentDamageReportedHandler(
    IMemberAccountRepository accounts, ILogger<EquipmentDamageReportedHandler> logger)
    : IIntegrationEventHandler<EquipmentDamageReportedIntegrationEvent>
{
    public async Task HandleAsync(
        EquipmentDamageReportedIntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        var account = await accounts.LoadRequiredAsync(integrationEvent.MemberId, cancellationToken).ConfigureAwait(false);
        var replacement = integrationEvent.ReplacementValue;
        var charge = integrationEvent.Condition == UnitCondition.Lost
            ? replacement
            : new Money(replacement.Amount / 2, replacement.Currency);
        account.PostCharge(charge, $"{integrationEvent.Condition} unit {integrationEvent.UnitId:N} on loan {integrationEvent.LoanId:N}", integrationEvent.OccurredAt);
        await accounts.SaveAsync(account, receipt: null, cancellationToken).ConfigureAwait(false);
        LogCharged(logger, charge.Amount, integrationEvent.Condition, integrationEvent.LoanId);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Charged {Amount} for a {Condition} unit on loan {LoanId}")]
    private static partial void LogCharged(ILogger logger, decimal amount, UnitCondition condition, Guid loanId);
}
