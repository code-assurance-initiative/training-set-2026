using Rentals.Lending.Domain.Loans;
using Rentals.Lending.Domain.Members;

namespace Rentals.Lending.Application.Loans;

public sealed record ExtendLoanCommand(LoanId LoanId, MemberId MemberId, DateTimeOffset NewDueAt);
