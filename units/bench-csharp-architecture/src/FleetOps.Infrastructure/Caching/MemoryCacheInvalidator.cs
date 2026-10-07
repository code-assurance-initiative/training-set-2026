using Microsoft.Extensions.Caching.Memory;

namespace FleetOps.Infrastructure.Caching;

/// <summary>Drops a vehicle's cached read model entry so the next read sees the change.</summary>
public sealed class MemoryCacheInvalidator(IMemoryCache cache) : ICacheInvalidator
{
    public void VehicleChanged(Guid vehicleId) => cache.Remove(CacheKeys.Vehicle(vehicleId));
}
