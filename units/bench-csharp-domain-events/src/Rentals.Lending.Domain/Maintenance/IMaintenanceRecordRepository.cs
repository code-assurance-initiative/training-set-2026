using Rentals.Lending.Domain.Catalogue;
using Rentals.SharedKernel;

namespace Rentals.Lending.Domain.Maintenance;

public interface IMaintenanceRecordRepository : IRepository<MaintenanceRecord, MaintenanceRecordId>
{
    Task<IReadOnlyList<MaintenanceRecord>> ListForUnitAsync(EquipmentUnitId unitId, CancellationToken cancellationToken);
}
