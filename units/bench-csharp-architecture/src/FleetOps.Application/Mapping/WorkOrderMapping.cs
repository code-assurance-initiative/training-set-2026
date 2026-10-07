using FleetOps.Contracts.WorkOrders;
using FleetOps.Domain.WorkOrders;

namespace FleetOps.Application.Mapping;

public static class WorkOrderMapping
{
    public static WorkOrderSummary ToSummary(this WorkOrder workOrder)
    {
        ArgumentNullException.ThrowIfNull(workOrder);
        return new WorkOrderSummary(
            workOrder.Id.Value,
            workOrder.VehicleId.Value,
            workOrder.Title,
            workOrder.Status.ToString(),
            [.. workOrder.Lines.Select(l => new WorkOrderLineSummary(l.Kind.ToString(), l.Description, l.Quantity, l.UnitPrice))],
            workOrder.ApprovedTotal,
            workOrder.OpenedAt,
            workOrder.LastViewedAt);
    }
}
