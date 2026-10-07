using Rentals.Lending.Domain.Members;
using Rentals.Messaging;
using Rentals.SharedKernel;

namespace Rentals.Lending.Application.Members;

/// <summary>Registers a member. A retry with the same contact details is accepted; another member's e-mail is not.</summary>
public sealed class RegisterMemberHandler(IMemberRepository members, IUnitOfWork unitOfWork, TimeProvider clock)
    : ICommandHandler<RegisterMemberCommand>
{
    public async Task HandleAsync(RegisterMemberCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var existing = await members.FindByEmailAsync(command.Email.Trim().ToLowerInvariant(), cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            if (existing.HasSameContact(command.FullName, command.Email, command.Phone))
            {
                return;
            }

            throw new DomainRuleViolationException("Another member is registered with this e-mail address.");
        }

        var member = Member.Register(command.FullName, command.Email, command.Phone, clock.GetUtcNow());
        await members.AddAsync(member, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
