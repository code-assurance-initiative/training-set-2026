namespace Rentals.Lending.Application.Loans;

public sealed record OverdueLoansQuery;

public sealed record OverdueLoan(Guid LoanId, Guid MemberId, DateTimeOffset DueAt);
