using FleetOps.Application.Mapping;
using FleetOps.Contracts.WorkOrders;
using FleetOps.Domain.Common;
using FleetOps.Domain.WorkOrders;
using Mediator;

namespace FleetOps.Application.Features.WorkOrders;

public sealed record ApproveWorkOrderCommand(Guid WorkOrderId, decimal ApprovedTotal) : ICommand<WorkOrderSummary>;

public sealed class ApproveWorkOrderHandler(IWorkOrderRepository workOrders, IUnitOfWork unitOfWork)
    : ICommandHandler<ApproveWorkOrderCommand, WorkOrderSummary>
{
    public async ValueTask<WorkOrderSummary> Handle(ApproveWorkOrderCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var workOrder = await workOrders.FindAsync(new WorkOrderId(command.WorkOrderId), cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException($"Work order {command.WorkOrderId} does not exist.");
        workOrder.Approve(command.ApprovedTotal);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return workOrder.ToSummary();
    }
}
