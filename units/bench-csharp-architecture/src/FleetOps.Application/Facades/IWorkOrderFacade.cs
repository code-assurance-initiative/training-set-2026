using FleetOps.Contracts.WorkOrders;
using FleetOps.Domain.WorkOrders;

namespace FleetOps.Application.Facades;

/// <summary>The work-order operations the API needs.</summary>
public interface IWorkOrderFacade
{
    Task<WorkOrderSummary> OpenAsync(Guid vehicleId, string title, CancellationToken cancellationToken);

    Task<WorkOrderSummary> AddLineAsync(Guid workOrderId, LineKind kind, string description, decimal quantity, decimal unitPrice, CancellationToken cancellationToken);

    Task<WorkOrderSummary?> GetAsync(Guid workOrderId, CancellationToken cancellationToken);

    Task<WorkOrderSummary> ApproveAsync(Guid workOrderId, decimal approvedTotal, CancellationToken cancellationToken);
}
