namespace Rentals.Lending.Application.Members;

public sealed record SearchMembersQuery(string NameFragment, int Take = 20);

public sealed record MemberSummary(Guid MemberId, string FullName, string Status);
