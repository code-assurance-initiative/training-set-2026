using FleetOps.Application.Abstractions;
using FleetOps.Application.Telemetry;
using FleetOps.Contracts.Vehicles;
using Mediator;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FleetOps.Application.Features.Vehicles;

public sealed record GetVehicleQuery(Guid VehicleId) : IQuery<VehicleSummary?>;

public sealed partial class GetVehicleHandler(
    IVehicleReadModel vehicles,
    IMemoryCache cache,
    FleetMetrics metrics,
    ILogger<GetVehicleHandler> logger) : IQueryHandler<GetVehicleQuery, VehicleSummary?>
{
    private static readonly TimeSpan CacheFor = TimeSpan.FromSeconds(30);

    public async ValueTask<VehicleSummary?> Handle(GetVehicleQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        metrics.VehicleLookups.Add(1);
        var summary = await cache.GetOrCreateAsync(
            ("vehicle", query.VehicleId),
            entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheFor;
                return vehicles.GetVehicleAsync(query.VehicleId, cancellationToken);
            }).ConfigureAwait(false);
        if (summary is null)
        {
            LogNotFound(query.VehicleId);
        }

        return summary;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Vehicle {VehicleId} not found")]
    private partial void LogNotFound(Guid vehicleId);
}
