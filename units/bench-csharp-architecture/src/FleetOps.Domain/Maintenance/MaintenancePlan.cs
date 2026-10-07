namespace FleetOps.Domain.Maintenance;

/// <summary>
/// The service intervals of the fleet. A service is due when the odometer has crossed a multiple of its interval
/// since the vehicle was last serviced.
/// </summary>
public sealed class MaintenancePlan(IReadOnlyList<ServiceInterval> intervals)
{
    public static MaintenancePlan Standard { get; } = new(
    [
        new ServiceInterval("Oil and filters", 15_000),
        new ServiceInterval("Brake inspection", 30_000),
        new ServiceInterval("Coolant and belts", 60_000),
        new ServiceInterval("Timing belt", 120_000),
    ]);

    public IReadOnlyList<ServiceInterval> Intervals { get; } = intervals;

    public IReadOnlyList<ServiceInterval> DueServices(int odometerKm, int lastServiceKm) =>
        [.. Intervals.Where(i => odometerKm / i.EveryKm > lastServiceKm / i.EveryKm)];
}
