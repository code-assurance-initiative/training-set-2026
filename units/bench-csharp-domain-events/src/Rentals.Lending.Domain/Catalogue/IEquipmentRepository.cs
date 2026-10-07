using Rentals.SharedKernel;

namespace Rentals.Lending.Domain.Catalogue;

public interface IEquipmentRepository : IRepository<Equipment, EquipmentId>
{
    Task<Equipment?> FindByUnitAsync(EquipmentUnitId unitId, CancellationToken cancellationToken);
}
