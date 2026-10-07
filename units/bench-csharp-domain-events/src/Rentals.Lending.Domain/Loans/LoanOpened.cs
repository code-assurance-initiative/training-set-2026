using Rentals.Lending.Domain.Catalogue;
using Rentals.Lending.Domain.Members;
using Rentals.SharedKernel;

namespace Rentals.Lending.Domain.Loans;

public sealed record LoanOpened(
    LoanId LoanId,
    MemberId MemberId,
    EquipmentId EquipmentId,
    EquipmentUnitId EquipmentUnitId,
    DateTimeOffset DueAt,
    DateTimeOffset OccurredAt) : IDomainEvent;
