using Rentals.Lending.Domain.Members;

namespace Rentals.Lending.Application.Members;

public sealed record SuspendMemberCommand(MemberId MemberId, string Reason);
