using Microsoft.Extensions.Logging;
using Rentals.Lending.Domain.Catalogue;
using Rentals.Lending.Domain.Loans;
using Rentals.Messaging;
using Rentals.SharedKernel;

namespace Rentals.Lending.Application.Loans;

/// <summary>
/// Closes a loan. The unit's condition is updated from the published damage event, in its own transaction; here the
/// equipment is only read, for the replacement value the damage event carries.
/// </summary>
public sealed partial class ReturnEquipmentHandler(
    ILoanRepository loans,
    IEquipmentRepository equipment,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    ILogger<ReturnEquipmentHandler> logger)
    : ICommandHandler<ReturnEquipmentCommand>
{
    public async Task HandleAsync(ReturnEquipmentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var loan = await loans.GetAsync(command.LoanId, cancellationToken).ConfigureAwait(false)
            ?? throw NotFoundException.For("Loan", command.LoanId);
        if (loan.Status == LoanStatus.Returned)
        {
            return;
        }

        var item = await equipment.GetAsync(loan.EquipmentId, cancellationToken).ConfigureAwait(false)
            ?? throw NotFoundException.For("Equipment", loan.EquipmentId);
        loan.Return(command.Condition, clock.GetUtcNow(), item.ReplacementValue, item.DailyRate);
        await loans.UpdateAsync(loan, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogReturned(logger, loan.Id.Value, command.Condition);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Loan {LoanId} returned, unit {Condition}")]
    private static partial void LogReturned(ILogger logger, Guid loanId, UnitCondition condition);
}
