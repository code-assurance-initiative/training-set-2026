using FleetOps.Application.Mapping;
using FleetOps.Contracts.WorkOrders;
using FleetOps.Domain.Common;
using FleetOps.Domain.WorkOrders;
using Mediator;

namespace FleetOps.Application.Features.WorkOrders;

public sealed record AddWorkOrderLineCommand(Guid WorkOrderId, LineKind Kind, string Description, decimal Quantity, decimal UnitPrice)
    : ICommand<WorkOrderSummary>;

public sealed class AddWorkOrderLineHandler(IWorkOrderRepository workOrders, IUnitOfWork unitOfWork)
    : ICommandHandler<AddWorkOrderLineCommand, WorkOrderSummary>
{
    public async ValueTask<WorkOrderSummary> Handle(AddWorkOrderLineCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var workOrder = await workOrders.FindAsync(new WorkOrderId(command.WorkOrderId), cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException($"Work order {command.WorkOrderId} does not exist.");
        workOrder.AddLine(new WorkOrderLine(command.Kind, command.Description, command.Quantity, command.UnitPrice));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return workOrder.ToSummary();
    }
}
