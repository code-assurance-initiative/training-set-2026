namespace FleetOps.Infrastructure.Caching;

public interface ICacheInvalidator
{
    void VehicleChanged(Guid vehicleId);
}
