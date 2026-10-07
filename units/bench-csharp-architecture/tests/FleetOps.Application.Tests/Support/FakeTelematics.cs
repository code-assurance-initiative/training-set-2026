using FleetOps.Contracts.Vehicles;
using FleetOps.Infrastructure.Telematics;

namespace FleetOps.Application.Tests.Support;

public sealed class FakeTelematics : ITelematicsClient
{
    public Dictionary<string, int> Odometers { get; } = new(StringComparer.Ordinal);

    public Task<OdometerReading> GetOdometerAsync(string vin, CancellationToken cancellationToken) =>
        Task.FromResult(new OdometerReading(vin, Odometers.GetValueOrDefault(vin), DateTimeOffset.UnixEpoch));

    public Task<VehiclePosition?> GetPositionAsync(string vin, CancellationToken cancellationToken) =>
        Task.FromResult<VehiclePosition?>(null);

    public Task<bool> PingAsync(CancellationToken cancellationToken) => Task.FromResult(true);
}
