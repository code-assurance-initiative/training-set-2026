using FleetOps.Contracts.Vehicles;

namespace FleetOps.Infrastructure.Telematics;

/// <summary>The telematics vendor's REST API, keyed by vehicle identification number.</summary>
public interface ITelematicsClient
{
    Task<OdometerReading> GetOdometerAsync(string vin, CancellationToken cancellationToken);

    Task<VehiclePosition?> GetPositionAsync(string vin, CancellationToken cancellationToken);

    Task<bool> PingAsync(CancellationToken cancellationToken);
}
