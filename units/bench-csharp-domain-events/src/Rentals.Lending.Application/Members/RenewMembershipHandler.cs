using Rentals.Lending.Domain.Members;
using Rentals.Messaging;
using Rentals.SharedKernel;

namespace Rentals.Lending.Application.Members;

public sealed class RenewMembershipHandler(IMemberRepository members, IUnitOfWork unitOfWork)
    : ICommandHandler<RenewMembershipCommand>
{
    public async Task HandleAsync(RenewMembershipCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var member = await members.GetAsync(command.MemberId, cancellationToken).ConfigureAwait(false)
            ?? throw NotFoundException.For("Member", command.MemberId);
        member.RenewMembership();
        await members.UpdateAsync(member, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
