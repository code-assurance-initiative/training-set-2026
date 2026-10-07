using Rentals.Lending.Domain.Catalogue;
using Rentals.Lending.Domain.Maintenance;

namespace Rentals.Lending.Application.Catalogue;

public sealed record RecordMaintenanceCommand(
    MaintenanceRecordId RecordId,
    EquipmentUnitId UnitId,
    string Description,
    decimal EstimatedCost);
