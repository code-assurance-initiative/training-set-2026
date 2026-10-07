using FleetOps.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Infrastructure.Persistence;

public sealed class WorkOrderRepository(FleetOpsDbContext db) : IWorkOrderRepository
{
    public Task<WorkOrder?> FindAsync(WorkOrderId id, CancellationToken cancellationToken) =>
        db.WorkOrders.SingleOrDefaultAsync(w => w.Id == id, cancellationToken);

    public void Add(WorkOrder workOrder) => db.WorkOrders.Add(workOrder);
}
