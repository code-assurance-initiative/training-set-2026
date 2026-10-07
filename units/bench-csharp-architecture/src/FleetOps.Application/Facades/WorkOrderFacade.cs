using FleetOps.Application.Features.WorkOrders;
using FleetOps.Contracts.WorkOrders;
using FleetOps.Domain.WorkOrders;
using Mediator;

namespace FleetOps.Application.Facades;

public sealed class WorkOrderFacade(ISender sender) : IWorkOrderFacade
{
    public async Task<WorkOrderSummary> OpenAsync(Guid vehicleId, string title, CancellationToken cancellationToken) =>
        await sender.Send(new OpenWorkOrderCommand(vehicleId, title), cancellationToken).ConfigureAwait(false);

    public async Task<WorkOrderSummary> AddLineAsync(
        Guid workOrderId, LineKind kind, string description, decimal quantity, decimal unitPrice, CancellationToken cancellationToken) =>
        await sender.Send(new AddWorkOrderLineCommand(workOrderId, kind, description, quantity, unitPrice), cancellationToken).ConfigureAwait(false);

    public async Task<WorkOrderSummary?> GetAsync(Guid workOrderId, CancellationToken cancellationToken) =>
        await sender.Send(new GetWorkOrderQuery(workOrderId), cancellationToken).ConfigureAwait(false);

    public async Task<WorkOrderSummary> ApproveAsync(Guid workOrderId, decimal approvedTotal, CancellationToken cancellationToken) =>
        await sender.Send(new ApproveWorkOrderCommand(workOrderId, approvedTotal), cancellationToken).ConfigureAwait(false);
}
