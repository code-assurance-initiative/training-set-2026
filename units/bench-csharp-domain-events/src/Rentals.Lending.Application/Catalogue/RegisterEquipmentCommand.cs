using Rentals.Lending.Domain.Catalogue;
using Rentals.SharedKernel;

namespace Rentals.Lending.Application.Catalogue;

public sealed record RegisterEquipmentCommand(
    EquipmentId EquipmentId,
    string Name,
    Money DailyRate,
    Money ReplacementValue,
    IReadOnlyList<string> SerialNumbers);
