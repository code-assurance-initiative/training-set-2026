using FleetOps.Contracts.WorkOrders;

namespace FleetOps.Application.Abstractions;

public interface IWorkOrderReadModel
{
    Task<WorkOrderSummary?> GetWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkOrderSummary>> ListForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkOrderSummary>> ListOpenAsync(CancellationToken cancellationToken);

    Task<int> CountOpenAsync(CancellationToken cancellationToken);

    Task<int> CountAwaitingApprovalAsync(CancellationToken cancellationToken);
}
