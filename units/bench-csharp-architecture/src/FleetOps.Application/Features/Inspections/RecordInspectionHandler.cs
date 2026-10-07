using FleetOps.Application.Features.WorkOrders;
using FleetOps.Contracts.Inspections;
using FleetOps.Domain.Common;
using FleetOps.Domain.Inspections;
using FleetOps.Domain.Vehicles;
using Mediator;

namespace FleetOps.Application.Features.Inspections;

public sealed record RecordInspectionCommand(Guid VehicleId, IReadOnlyList<Defect> Defects) : ICommand<InspectionSummary>;

public sealed class RecordInspectionHandler : ICommandHandler<RecordInspectionCommand, InspectionSummary>
{
    private readonly IInspectionRepository _inspections;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
    private readonly OpenWorkOrderHandler _openWorkOrder;

    public RecordInspectionHandler(
        IInspectionRepository inspections,
        IUnitOfWork unitOfWork,
        TimeProvider clock,
        OpenWorkOrderHandler openWorkOrder)
    {
        _inspections = inspections;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _openWorkOrder = openWorkOrder;
    }

    public async ValueTask<InspectionSummary> Handle(RecordInspectionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var inspection = Inspection.Record(new VehicleId(command.VehicleId), _clock.GetUtcNow(), command.Defects);
        _inspections.Add(inspection);

        if (!inspection.Passed)
        {
            var offRoad = inspection.Defects.Where(d => d.TakesVehicleOffRoad).Select(d => d.Description);
            var workOrder = await _openWorkOrder
                .Handle(new OpenWorkOrderCommand(command.VehicleId, "Repair: " + string.Join("; ", offRoad)), cancellationToken)
                .ConfigureAwait(false);
            inspection.LinkFollowUp(new Domain.WorkOrders.WorkOrderId(workOrder.Id));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new InspectionSummary(
            inspection.Id.Value,
            inspection.VehicleId.Value,
            inspection.InspectedAt,
            inspection.Passed,
            [.. inspection.Defects.Select(d => $"{d.Severity}: {d.Description}")],
            inspection.FollowUpWorkOrderId?.Value);
    }
}
