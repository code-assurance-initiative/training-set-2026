using Rentals.Lending.Domain.Loans;
using Rentals.Messaging;

namespace Rentals.Lending.Application.Loans;

public sealed class OverdueLoansHandler(ILoanRepository loans, TimeProvider clock)
    : IQueryHandler<OverdueLoansQuery, IReadOnlyList<OverdueLoan>>
{
    public async Task<IReadOnlyList<OverdueLoan>> HandleAsync(OverdueLoansQuery query, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var open = await loans.ListOpenAsync(cancellationToken).ConfigureAwait(false);
        return [.. open.Where(l => l.IsOverdueAt(now)).Select(l => new OverdueLoan(l.Id.Value, l.Borrower.Id.Value, l.DueAt))];
    }
}
