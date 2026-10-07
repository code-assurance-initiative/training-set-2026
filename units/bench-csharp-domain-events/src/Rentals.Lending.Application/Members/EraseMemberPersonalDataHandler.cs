using Rentals.Lending.Domain.Loans;
using Rentals.Lending.Domain.Members;
using Rentals.Messaging;
using Rentals.SharedKernel;

namespace Rentals.Lending.Application.Members;

/// <summary>Erases a member's personal data (GDPR right to erasure) once nothing is on loan to them.</summary>
public sealed class EraseMemberPersonalDataHandler(
    IMemberRepository members, ILoanRepository loans, IUnitOfWork unitOfWork, TimeProvider clock)
    : ICommandHandler<EraseMemberPersonalDataCommand>
{
    public async Task HandleAsync(EraseMemberPersonalDataCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var member = await members.GetAsync(command.MemberId, cancellationToken).ConfigureAwait(false)
            ?? throw NotFoundException.For("Member", command.MemberId);
        var openLoans = await loans.ListOpenForMemberAsync(member.Id, cancellationToken).ConfigureAwait(false);
        if (openLoans.Count > 0)
        {
            throw new DomainRuleViolationException("Personal data is kept while equipment is on loan to the member.");
        }

        member.ErasePersonalData(clock.GetUtcNow());
        await members.UpdateAsync(member, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
