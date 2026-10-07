using System.Diagnostics.Metrics;

namespace FleetOps.Application.Telemetry;

/// <summary>The application's counters, published on the <c>FleetOps</c> meter.</summary>
public sealed class FleetMetrics
{
    public const string MeterName = "FleetOps";

    public FleetMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);
        var meter = meterFactory.Create(MeterName);
        VehicleLookups = meter.CreateCounter<long>("fleetops.vehicle.lookups", description: "Vehicle detail reads.");
    }

    public Counter<long> VehicleLookups { get; }
}
