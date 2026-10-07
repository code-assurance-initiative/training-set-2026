using Microsoft.EntityFrameworkCore;
using Rentals.Lending.Domain.Members;

namespace Rentals.Lending.Infrastructure.Persistence.Repositories;

internal sealed class MemberRepository(LendingDbContext db) : IMemberRepository
{
    public Task<Member?> GetAsync(MemberId id, CancellationToken cancellationToken) =>
        db.Members.SingleOrDefaultAsync(m => m.Id == id, cancellationToken);

    public Task<Member?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        db.Members.SingleOrDefaultAsync(m => m.Email == email, cancellationToken);

    public IQueryable<Member> Query() => db.Members.AsNoTracking();

    public async Task AddAsync(Member entity, CancellationToken cancellationToken) =>
        await db.Members.AddAsync(entity, cancellationToken).ConfigureAwait(false);

    public Task UpdateAsync(Member entity, CancellationToken cancellationToken)
    {
        db.Members.Update(entity);
        return Task.CompletedTask;
    }
}
