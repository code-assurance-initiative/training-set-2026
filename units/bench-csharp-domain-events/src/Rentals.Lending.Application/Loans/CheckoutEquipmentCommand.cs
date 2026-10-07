using Rentals.Lending.Domain.Catalogue;
using Rentals.Lending.Domain.Members;

namespace Rentals.Lending.Application.Loans;

public sealed record CheckoutEquipmentCommand(MemberId MemberId, EquipmentUnitId UnitId, int Days);
