using FleetOps.Application.Mapping;
using FleetOps.Contracts.WorkOrders;
using FleetOps.Domain.Common;
using FleetOps.Domain.Vehicles;
using FleetOps.Domain.WorkOrders;
using Mediator;

namespace FleetOps.Application.Features.WorkOrders;

public sealed record OpenWorkOrderCommand(Guid VehicleId, string Title) : ICommand<WorkOrderSummary>;

public sealed class OpenWorkOrderHandler(
    IVehicleRepository vehicles,
    IWorkOrderRepository workOrders,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<OpenWorkOrderCommand, WorkOrderSummary>
{
    public async ValueTask<WorkOrderSummary> Handle(OpenWorkOrderCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var vehicle = await vehicles.FindAsync(new VehicleId(command.VehicleId), cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException($"Vehicle {command.VehicleId} does not exist.");
        var workOrder = WorkOrder.Open(vehicle.Id, command.Title, clock.GetUtcNow());
        vehicle.SendToWorkshop();
        workOrders.Add(workOrder);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return workOrder.ToSummary();
    }
}
