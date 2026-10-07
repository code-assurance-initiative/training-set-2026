using FleetOps.Domain.Common;
using FleetOps.Domain.Vehicles;

namespace FleetOps.Domain.WorkOrders;

public sealed class WorkOrder
{
    private readonly List<WorkOrderLine> _lines = [];

    private WorkOrder(WorkOrderId id, VehicleId vehicleId, string title, DateTimeOffset openedAt)
    {
        Id = id;
        VehicleId = vehicleId;
        Title = title;
        OpenedAt = openedAt;
        Status = WorkOrderStatus.Open;
    }

    public WorkOrderId Id { get; private set; }

    public VehicleId VehicleId { get; private set; }

    public string Title { get; private set; }

    public WorkOrderStatus Status { get; private set; }

    public IReadOnlyList<WorkOrderLine> Lines => _lines;

    public decimal? ApprovedTotal { get; private set; }

    public DateTimeOffset OpenedAt { get; private set; }

    public DateTimeOffset? LastViewedAt { get; private set; }

    public static WorkOrder Open(VehicleId vehicleId, string title, DateTimeOffset openedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        return new WorkOrder(WorkOrderId.New(), vehicleId, title.Trim(), openedAt);
    }

    public void AddLine(WorkOrderLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        if (Status is not (WorkOrderStatus.Open or WorkOrderStatus.Quoted))
        {
            throw new DomainException($"Lines cannot be added to a work order that is {Status}.");
        }

        if (line.Quantity <= 0 || line.UnitPrice < 0)
        {
            throw new DomainException("A line needs a positive quantity and a non-negative price.");
        }

        _lines.Add(line);
        Status = WorkOrderStatus.Quoted;
    }

    public void Approve(decimal total)
    {
        if (Status != WorkOrderStatus.Quoted)
        {
            throw new DomainException($"Only a quoted work order can be approved; this one is {Status}.");
        }

        ApprovedTotal = total;
        Status = WorkOrderStatus.Approved;
    }

    public void Complete()
    {
        if (Status != WorkOrderStatus.Approved)
        {
            throw new DomainException($"Only an approved work order can be completed; this one is {Status}.");
        }

        Status = WorkOrderStatus.Completed;
    }

    public void MarkViewed(DateTimeOffset at) => LastViewedAt = at;
}
