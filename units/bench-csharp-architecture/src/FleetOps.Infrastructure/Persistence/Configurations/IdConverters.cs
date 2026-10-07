using FleetOps.Domain.Inspections;
using FleetOps.Domain.Vehicles;
using FleetOps.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FleetOps.Infrastructure.Persistence.Configurations;

/// <summary>Strongly-typed ids are stored as their Guid.</summary>
public static class IdConverters
{
    public static ValueConverter<VehicleId, Guid> Vehicle { get; } = new(id => id.Value, value => new VehicleId(value));

    public static ValueConverter<WorkOrderId, Guid> WorkOrder { get; } = new(id => id.Value, value => new WorkOrderId(value));

    public static ValueConverter<InspectionId, Guid> Inspection { get; } = new(id => id.Value, value => new InspectionId(value));
}
