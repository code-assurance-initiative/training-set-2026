using FleetOps.Domain.Vehicles;
using FleetOps.Domain.WorkOrders;

namespace FleetOps.Domain.Inspections;

public sealed class Inspection
{
    private readonly List<Defect> _defects = [];

    private Inspection(InspectionId id, VehicleId vehicleId, DateTimeOffset inspectedAt)
    {
        Id = id;
        VehicleId = vehicleId;
        InspectedAt = inspectedAt;
    }

    public InspectionId Id { get; private set; }

    public VehicleId VehicleId { get; private set; }

    public DateTimeOffset InspectedAt { get; private set; }

    public IReadOnlyList<Defect> Defects => _defects;

    public WorkOrderId? FollowUpWorkOrderId { get; private set; }

    public bool Passed => !_defects.Any(d => d.TakesVehicleOffRoad);

    public static Inspection Record(VehicleId vehicleId, DateTimeOffset inspectedAt, IEnumerable<Defect> defects)
    {
        ArgumentNullException.ThrowIfNull(defects);
        var inspection = new Inspection(InspectionId.New(), vehicleId, inspectedAt);
        inspection._defects.AddRange(defects);
        return inspection;
    }

    public void LinkFollowUp(WorkOrderId workOrderId) => FollowUpWorkOrderId = workOrderId;
}
