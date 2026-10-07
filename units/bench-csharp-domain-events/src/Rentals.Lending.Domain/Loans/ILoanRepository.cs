using Rentals.Lending.Domain.Members;
using Rentals.SharedKernel;

namespace Rentals.Lending.Domain.Loans;

public interface ILoanRepository : IRepository<Loan, LoanId>
{
    Task<IReadOnlyList<Loan>> ListOpenAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Loan>> ListOpenForMemberAsync(MemberId memberId, CancellationToken cancellationToken);
}
