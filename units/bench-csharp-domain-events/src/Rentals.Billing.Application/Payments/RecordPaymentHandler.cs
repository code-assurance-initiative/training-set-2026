using Rentals.Billing.Application.Handlers;
using Rentals.Billing.Domain.Accounts;
using Rentals.Messaging;
using Rentals.SharedKernel;

namespace Rentals.Billing.Application.Payments;

/// <summary>Records a payment against a member's account. The payment provider's reference makes a retry a no-op.</summary>
public sealed class RecordPaymentHandler(IMemberAccountRepository accounts, TimeProvider clock)
    : ICommandHandler<RecordPaymentCommand>
{
    public async Task HandleAsync(RecordPaymentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var account = await accounts.LoadRequiredAsync(command.MemberId, cancellationToken).ConfigureAwait(false);
        account.RecordPayment(new Money(command.Amount, command.Currency), command.PaymentReference, clock.GetUtcNow());
        await accounts.SaveAsync(account, receipt: null, cancellationToken).ConfigureAwait(false);
    }
}
