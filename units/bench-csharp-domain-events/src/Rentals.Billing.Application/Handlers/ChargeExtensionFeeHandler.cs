using Microsoft.Extensions.Options;
using Rentals.Billing.Domain.Accounts;
using Rentals.Lending.Application.Loans;
using Rentals.Messaging;
using Rentals.SharedKernel;

namespace Rentals.Billing.Application.Handlers;

/// <summary>Charges the extension fee when a loan is extended. One fee per loan and new due date.</summary>
public sealed class ChargeExtensionFeeHandler(IMemberAccountRepository accounts, IOptions<BillingOptions> options, TimeProvider clock)
    : ICommandHandler<ExtendLoanCommand>
{
    public async Task HandleAsync(ExtendLoanCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var account = await accounts.LoadRequiredAsync(command.MemberId.Value, cancellationToken).ConfigureAwait(false);
        var reason = $"Extension of loan {command.LoanId} to {command.NewDueAt:yyyy-MM-dd}";
        if (account.HasChargeWithReason(reason))
        {
            return;
        }

        account.PostCharge(new Money(options.Value.ExtensionFee, account.Currency), reason, clock.GetUtcNow());
        await accounts.SaveAsync(account, receipt: null, cancellationToken).ConfigureAwait(false);
    }
}
