using Rentals.Lending.Domain.Members;
using Rentals.Messaging;

namespace Rentals.Lending.Application.Members;

public sealed class SearchMembersHandler(IMemberRepository members)
    : IQueryHandler<SearchMembersQuery, IReadOnlyList<MemberSummary>>
{
    public Task<IReadOnlyList<MemberSummary>> HandleAsync(SearchMembersQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        IReadOnlyList<MemberSummary> found = [.. members.Query()
            .Where(m => m.FullName.Contains(query.NameFragment))
            .OrderBy(m => m.FullName)
            .Take(Math.Clamp(query.Take, 1, 100))
            .Select(m => new MemberSummary(m.Id.Value, m.FullName, m.Status.ToString()))];
        return Task.FromResult(found);
    }
}
