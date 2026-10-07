using FleetOps.Domain.Vehicles;
using FleetOps.Infrastructure.Telematics;

namespace FleetOps.Domain.Maintenance;

/// <summary>Decides which services a vehicle is due for, from its live odometer.</summary>
public sealed class MaintenanceDueEvaluator(ITelematicsClient telematics)
{
    public async Task<MaintenanceAssessment> EvaluateAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        var reading = await telematics.GetOdometerAsync(vehicle.Vin.Value, cancellationToken).ConfigureAwait(false);
        var currentKm = Math.Max(reading.Kilometres, vehicle.OdometerKm);
        return new MaintenanceAssessment(currentKm, MaintenancePlan.Standard.DueServices(currentKm, vehicle.LastServiceKm));
    }
}
