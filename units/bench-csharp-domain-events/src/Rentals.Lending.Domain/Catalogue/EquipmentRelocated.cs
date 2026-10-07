using Rentals.SharedKernel;

namespace Rentals.Lending.Domain.Catalogue;

/// <summary>
/// An equipment type was moved to another branch or shelf. Raised so that members holding a reservation for it are
/// told where to collect it.
/// </summary>
public sealed record EquipmentRelocated(EquipmentId EquipmentId, string Branch, string Shelf, DateTimeOffset OccurredAt) : IDomainEvent;
