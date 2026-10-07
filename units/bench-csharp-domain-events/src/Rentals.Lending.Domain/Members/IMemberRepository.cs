using Rentals.SharedKernel;

namespace Rentals.Lending.Domain.Members;

public interface IMemberRepository : IRepository<Member, MemberId>
{
    Task<Member?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    IQueryable<Member> Query();
}
