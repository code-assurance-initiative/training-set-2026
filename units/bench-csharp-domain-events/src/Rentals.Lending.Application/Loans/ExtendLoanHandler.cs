using Rentals.Lending.Domain.Loans;
using Rentals.Messaging;
using Rentals.SharedKernel;

namespace Rentals.Lending.Application.Loans;

/// <summary>Moves a loan's due date. Extending to the date it already has changes nothing.</summary>
public sealed class ExtendLoanHandler(ILoanRepository loans, IUnitOfWork unitOfWork, TimeProvider clock)
    : ICommandHandler<ExtendLoanCommand>
{
    public async Task HandleAsync(ExtendLoanCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var loan = await loans.GetAsync(command.LoanId, cancellationToken).ConfigureAwait(false)
            ?? throw NotFoundException.For("Loan", command.LoanId);
        loan.Extend(command.NewDueAt, clock.GetUtcNow());
        await loans.UpdateAsync(loan, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
