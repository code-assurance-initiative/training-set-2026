using FleetOps.Contracts.Paging;
using FleetOps.Contracts.Vehicles;

namespace FleetOps.Application.Abstractions;

/// <summary>Read-side queries over vehicles, answered straight from storage as contracts.</summary>
public interface IVehicleReadModel
{
    Task<VehicleSummary?> GetVehicleAsync(Guid vehicleId, CancellationToken cancellationToken);

    Task<PagedResult<VehicleSummary>> ListVehiclesAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<int> CountActiveVehiclesAsync(CancellationToken cancellationToken);

    Task<int> CountVehiclesInWorkshopAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<VehicleSummary>> FindByRegistrationAsync(string registrationPrefix, CancellationToken cancellationToken);

    Task<IReadOnlyList<VehicleSummary>> ListOverdueForServiceAsync(int toleranceKm, CancellationToken cancellationToken);
}
