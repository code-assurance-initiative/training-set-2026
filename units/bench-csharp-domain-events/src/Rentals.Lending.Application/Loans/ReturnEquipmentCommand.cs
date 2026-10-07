using Rentals.Lending.Domain.Catalogue;
using Rentals.Lending.Domain.Loans;

namespace Rentals.Lending.Application.Loans;

public sealed record ReturnEquipmentCommand(LoanId LoanId, UnitCondition Condition);
