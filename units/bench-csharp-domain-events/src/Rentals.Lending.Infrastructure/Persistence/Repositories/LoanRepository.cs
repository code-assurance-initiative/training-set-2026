using Microsoft.EntityFrameworkCore;
using Rentals.Lending.Domain.Loans;
using Rentals.Lending.Domain.Members;

namespace Rentals.Lending.Infrastructure.Persistence.Repositories;

internal sealed class LoanRepository(LendingDbContext db) : ILoanRepository
{
    public Task<Loan?> GetAsync(LoanId id, CancellationToken cancellationToken) =>
        db.Loans.SingleOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Loan>> ListOpenAsync(CancellationToken cancellationToken) =>
        await db.Loans.Where(l => l.Status == LoanStatus.Open).ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<Loan>> ListOpenForMemberAsync(MemberId memberId, CancellationToken cancellationToken) =>
        await db.Loans
            .Where(l => l.Status == LoanStatus.Open && EF.Property<MemberId>(l, "BorrowerId") == memberId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(Loan entity, CancellationToken cancellationToken) =>
        await db.Loans.AddAsync(entity, cancellationToken).ConfigureAwait(false);

    public Task UpdateAsync(Loan entity, CancellationToken cancellationToken)
    {
        db.Loans.Update(entity);
        return Task.CompletedTask;
    }
}
