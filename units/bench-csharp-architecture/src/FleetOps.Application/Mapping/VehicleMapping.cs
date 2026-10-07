using FleetOps.Contracts.Vehicles;
using FleetOps.Domain.Vehicles;

namespace FleetOps.Application.Mapping;

public static class VehicleMapping
{
    public static VehicleSummary ToSummary(this Vehicle vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        return new VehicleSummary(
            vehicle.Id.Value,
            vehicle.Vin.Value,
            vehicle.Registration,
            vehicle.Model,
            vehicle.OdometerKm,
            vehicle.LastServiceKm,
            vehicle.Status.ToString());
    }
}
