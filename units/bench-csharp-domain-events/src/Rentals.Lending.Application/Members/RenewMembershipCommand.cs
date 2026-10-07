using Rentals.Lending.Domain.Members;

namespace Rentals.Lending.Application.Members;

public sealed record RenewMembershipCommand(MemberId MemberId);
