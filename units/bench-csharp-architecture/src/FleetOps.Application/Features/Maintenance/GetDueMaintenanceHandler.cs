using FleetOps.Contracts.Maintenance;
using FleetOps.Domain.Maintenance;
using FleetOps.Domain.Vehicles;
using Mediator;

namespace FleetOps.Application.Features.Maintenance;

public sealed record GetDueMaintenanceQuery : IQuery<IReadOnlyList<MaintenanceDue>>;

/// <summary>Every service the active fleet is due for, judged on each vehicle's live odometer.</summary>
public sealed class GetDueMaintenanceHandler : IQueryHandler<GetDueMaintenanceQuery, IReadOnlyList<MaintenanceDue>>
{
    private readonly IVehicleRepository _vehicles;
    private readonly MaintenanceDueEvaluator _evaluator;

    public GetDueMaintenanceHandler(IVehicleRepository vehicles, MaintenanceDueEvaluator evaluator)
    {
        _vehicles = vehicles;
        _evaluator = evaluator;
    }

    public async ValueTask<IReadOnlyList<MaintenanceDue>> Handle(GetDueMaintenanceQuery query, CancellationToken cancellationToken)
    {
        var due = new List<MaintenanceDue>();
        foreach (var vehicle in await _vehicles.ListActiveAsync(cancellationToken).ConfigureAwait(false))
        {
            var assessment = await _evaluator.EvaluateAsync(vehicle, cancellationToken).ConfigureAwait(false);
            due.AddRange(assessment.DueServices.Select(s =>
                new MaintenanceDue(vehicle.Id.Value, vehicle.Registration, s.Name, s.EveryKm, assessment.CurrentKm)));
        }

        return due;
    }
}
