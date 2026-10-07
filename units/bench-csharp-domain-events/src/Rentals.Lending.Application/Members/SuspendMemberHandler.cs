using Microsoft.Extensions.Logging;
using Rentals.Contracts.IntegrationEvents;
using Rentals.Lending.Domain.Members;
using Rentals.Messaging;
using Rentals.SharedKernel;

namespace Rentals.Lending.Application.Members;

/// <summary>Suspends a member and tells the other contexts straight away.</summary>
public sealed partial class SuspendMemberHandler(
    IMemberRepository members, IUnitOfWork unitOfWork, IMessageBus bus, TimeProvider clock, ILogger<SuspendMemberHandler> logger)
    : ICommandHandler<SuspendMemberCommand>
{
    public async Task HandleAsync(SuspendMemberCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var member = await members.GetAsync(command.MemberId, cancellationToken).ConfigureAwait(false)
            ?? throw NotFoundException.For("Member", command.MemberId);
        if (member.Status == MemberStatus.Suspended)
        {
            return;
        }

        var now = clock.GetUtcNow();
        member.Suspend(command.Reason, now);
        await members.UpdateAsync(member, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var suspended = new MemberSuspendedIntegrationEvent(member.Id.Value, command.Reason, now);
        var context = new MessageContext(Guid.NewGuid(), nameof(MemberSuspendedIntegrationEvent), now);
        await bus.PublishAsync(suspended, context, cancellationToken).ConfigureAwait(false);
        LogSuspended(logger, member.Id.Value);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Member {MemberId} suspended")]
    private static partial void LogSuspended(ILogger logger, Guid memberId);
}
