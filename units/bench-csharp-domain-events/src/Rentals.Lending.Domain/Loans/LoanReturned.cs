using Rentals.Lending.Domain.Catalogue;
using Rentals.Lending.Domain.Members;
using Rentals.SharedKernel;

namespace Rentals.Lending.Domain.Loans;

public sealed record LoanReturned(
    LoanId LoanId,
    MemberId MemberId,
    EquipmentId EquipmentId,
    EquipmentUnitId EquipmentUnitId,
    DateTimeOffset DueAt,
    UnitCondition ReturnCondition,
    Money ReplacementValue,
    Money DailyRate,
    DateTimeOffset OccurredAt) : IDomainEvent;
