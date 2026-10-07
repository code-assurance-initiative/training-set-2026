namespace FleetOps.Domain.WorkOrders;

public interface IWorkOrderRepository
{
    Task<WorkOrder?> FindAsync(WorkOrderId id, CancellationToken cancellationToken);

    void Add(WorkOrder workOrder);
}
