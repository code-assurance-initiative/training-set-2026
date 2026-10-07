using FleetOps.Application.Mapping;
using FleetOps.Contracts.WorkOrders;
using FleetOps.Domain.Common;
using FleetOps.Domain.WorkOrders;
using Mediator;

namespace FleetOps.Application.Features.WorkOrders;

public sealed record GetWorkOrderQuery(Guid WorkOrderId) : IQuery<WorkOrderSummary?>;

public sealed class GetWorkOrderHandler(IWorkOrderRepository workOrders, IUnitOfWork unitOfWork, TimeProvider clock)
    : IQueryHandler<GetWorkOrderQuery, WorkOrderSummary?>
{
    public async ValueTask<WorkOrderSummary?> Handle(GetWorkOrderQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var workOrder = await workOrders.FindAsync(new WorkOrderId(query.WorkOrderId), cancellationToken).ConfigureAwait(false);
        if (workOrder is null)
        {
            return null;
        }

        workOrder.MarkViewed(clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return workOrder.ToSummary();
    }
}
