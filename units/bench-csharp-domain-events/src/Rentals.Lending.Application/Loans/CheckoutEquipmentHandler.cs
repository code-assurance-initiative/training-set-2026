using Microsoft.Extensions.Logging;
using Rentals.Lending.Domain.Catalogue;
using Rentals.Lending.Domain.Loans;
using Rentals.Lending.Domain.Members;
using Rentals.Messaging;
using Rentals.SharedKernel;

namespace Rentals.Lending.Application.Loans;

/// <summary>Lends a unit to a member in good standing. A unit already on loan cannot be lent twice.</summary>
public sealed partial class CheckoutEquipmentHandler(
    IMemberRepository members,
    IEquipmentRepository equipment,
    ILoanRepository loans,
    UnitAvailabilityService availability,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    ILogger<CheckoutEquipmentHandler> logger) : ICommandHandler<CheckoutEquipmentCommand>
{
    public async Task HandleAsync(CheckoutEquipmentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var now = clock.GetUtcNow();
        var member = await members.GetAsync(command.MemberId, cancellationToken).ConfigureAwait(false)
            ?? throw NotFoundException.For("Member", command.MemberId);
        if (member.Status != MemberStatus.Active || member.MembershipExpiresAt <= now)
        {
            throw new DomainRuleViolationException("Only a member in good standing can borrow equipment.");
        }

        var openLoans = await loans.ListOpenForMemberAsync(member.Id, cancellationToken).ConfigureAwait(false);
        if (openLoans.Count >= member.LoanLimit)
        {
            throw new DomainRuleViolationException($"The member already has {openLoans.Count} items on loan.");
        }

        var item = await equipment.FindByUnitAsync(command.UnitId, cancellationToken).ConfigureAwait(false)
            ?? throw NotFoundException.For("Unit", command.UnitId);
        if (!await availability.IsAvailableAsync(item, command.UnitId, cancellationToken).ConfigureAwait(false))
        {
            throw new DomainRuleViolationException($"Unit {command.UnitId} is not available.");
        }

        var loan = Loan.Open(member, item.Id, command.UnitId, LoanPeriod.Starting(now, command.Days));
        member.RecordCheckout();
        await loans.AddAsync(loan, cancellationToken).ConfigureAwait(false);
        await members.UpdateAsync(member, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogLent(logger, command.UnitId.Value, member.Id.Value, loan.DueAt);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Unit {UnitId} lent to member {MemberId} until {DueAt}")]
    private static partial void LogLent(ILogger logger, Guid unitId, Guid memberId, DateTimeOffset dueAt);
}
