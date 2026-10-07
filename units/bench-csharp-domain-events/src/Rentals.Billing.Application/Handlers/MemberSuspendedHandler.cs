using Microsoft.Extensions.Options;
using Rentals.Contracts.IntegrationEvents;
using Rentals.Messaging;

namespace Rentals.Billing.Application.Handlers;

/// <summary>Asks the statements service to send a suspended member a final statement of what they owe.</summary>
public sealed class MemberSuspendedHandler(IMessageBus bus, IOptions<BillingOptions> options)
    : IIntegrationEventHandler<MemberSuspendedIntegrationEvent>
{
    public async Task HandleAsync(
        MemberSuspendedIntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        var command = new SendFinalStatementCommand(integrationEvent.MemberId, integrationEvent.Reason);
        await bus.SendAsync(command, options.Value.StatementsEndpoint, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handled by the statements service, outside this repository.</summary>
public sealed record SendFinalStatementCommand(Guid MemberId, string Reason);
