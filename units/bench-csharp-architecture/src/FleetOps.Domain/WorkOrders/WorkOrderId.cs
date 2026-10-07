namespace FleetOps.Domain.WorkOrders;

public readonly record struct WorkOrderId(Guid Value)
{
    public static WorkOrderId New() => new(Guid.NewGuid());
}
