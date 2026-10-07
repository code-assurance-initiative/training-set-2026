using Rentals.Lending.Domain.Catalogue;

namespace Rentals.Lending.Application.Catalogue;

public sealed record RelocateEquipmentCommand(EquipmentId EquipmentId, string Branch, string Shelf);
