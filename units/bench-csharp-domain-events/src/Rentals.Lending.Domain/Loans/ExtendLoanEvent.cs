using Rentals.SharedKernel;

namespace Rentals.Lending.Domain.Loans;

public sealed record ExtendLoanEvent(LoanId LoanId, DateTimeOffset NewDueAt, DateTimeOffset OccurredAt) : IDomainEvent;
