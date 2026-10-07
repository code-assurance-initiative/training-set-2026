using FleetOps.Application.Abstractions;
using FleetOps.Application.Mapping;
using FleetOps.Contracts.Inspections;
using FleetOps.Contracts.Paging;
using FleetOps.Contracts.Vehicles;
using FleetOps.Contracts.WorkOrders;
using FleetOps.Domain.Inspections;
using FleetOps.Domain.Vehicles;
using FleetOps.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Infrastructure.Persistence;

/// <summary>The read side over the same database, without change tracking.</summary>
public sealed class FleetReadModel(FleetOpsDbContext db) : IFleetReadModel
{
    private IQueryable<Vehicle> Vehicles => db.Vehicles.AsNoTracking();

    private IQueryable<WorkOrder> WorkOrders => db.WorkOrders.AsNoTracking();

    private IQueryable<Inspection> Inspections => db.Inspections.AsNoTracking();

    public async Task<VehicleSummary?> GetVehicleAsync(Guid vehicleId, CancellationToken cancellationToken) =>
        (await Vehicles.SingleOrDefaultAsync(v => v.Id == new VehicleId(vehicleId), cancellationToken).ConfigureAwait(false))?.ToSummary();

    public async Task<PagedResult<VehicleSummary>> ListVehiclesAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var total = await Vehicles.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await Vehicles.OrderBy(v => v.Registration).Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return new PagedResult<VehicleSummary>([.. items.Select(v => v.ToSummary())], page, pageSize, total);
    }

    public Task<int> CountActiveVehiclesAsync(CancellationToken cancellationToken) =>
        Vehicles.CountAsync(v => v.Status == VehicleStatus.Active, cancellationToken);

    public Task<int> CountVehiclesInWorkshopAsync(CancellationToken cancellationToken) =>
        Vehicles.CountAsync(v => v.Status == VehicleStatus.InWorkshop, cancellationToken);

    public async Task<IReadOnlyList<VehicleSummary>> FindByRegistrationAsync(string registrationPrefix, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(registrationPrefix);
        var prefix = registrationPrefix.Trim().ToUpperInvariant();
        var found = await Vehicles.Where(v => v.Registration.StartsWith(prefix)).OrderBy(v => v.Registration)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. found.Select(v => v.ToSummary())];
    }

    public async Task<IReadOnlyList<VehicleSummary>> ListOverdueForServiceAsync(int toleranceKm, CancellationToken cancellationToken)
    {
        var interval = Domain.Maintenance.MaintenancePlan.Standard.Intervals.Min(i => i.EveryKm);
        var overdue = await Vehicles.Where(v => v.OdometerKm - v.LastServiceKm > interval + toleranceKm)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. overdue.Select(v => v.ToSummary())];
    }

    public async Task<WorkOrderSummary?> GetWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken) =>
        (await WorkOrders.SingleOrDefaultAsync(w => w.Id == new WorkOrderId(workOrderId), cancellationToken).ConfigureAwait(false))?.ToSummary();

    public async Task<IReadOnlyList<WorkOrderSummary>> ListForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        var list = await WorkOrders.Where(w => w.VehicleId == new VehicleId(vehicleId)).OrderByDescending(w => w.OpenedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. list.Select(w => w.ToSummary())];
    }

    public async Task<IReadOnlyList<WorkOrderSummary>> ListOpenAsync(CancellationToken cancellationToken)
    {
        var list = await WorkOrders.Where(w => w.Status != WorkOrderStatus.Completed).OrderBy(w => w.OpenedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. list.Select(w => w.ToSummary())];
    }

    public Task<int> CountOpenAsync(CancellationToken cancellationToken) =>
        WorkOrders.CountAsync(w => w.Status != WorkOrderStatus.Completed, cancellationToken);

    public Task<int> CountAwaitingApprovalAsync(CancellationToken cancellationToken) =>
        WorkOrders.CountAsync(w => w.Status == WorkOrderStatus.Quoted, cancellationToken);

    public async Task<InspectionSummary?> GetInspectionAsync(Guid inspectionId, CancellationToken cancellationToken) =>
        ToSummary(await Inspections.SingleOrDefaultAsync(i => i.Id == new InspectionId(inspectionId), cancellationToken).ConfigureAwait(false));

    async Task<IReadOnlyList<InspectionSummary>> IInspectionReadModel.ListForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        var list = await Inspections.Where(i => i.VehicleId == new VehicleId(vehicleId)).OrderByDescending(i => i.InspectedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. list.Select(Summarise)];
    }

    public async Task<IReadOnlyList<InspectionSummary>> ListFailedSinceAsync(DateTimeOffset since, CancellationToken cancellationToken)
    {
        var list = await Inspections.Where(i => i.InspectedAt >= since).ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. list.Where(i => !i.Passed).Select(Summarise)];
    }

    public Task<int> CountSinceAsync(DateTimeOffset since, CancellationToken cancellationToken) =>
        Inspections.CountAsync(i => i.InspectedAt >= since, cancellationToken);

    public async Task<int> CountFailedSinceAsync(DateTimeOffset since, CancellationToken cancellationToken) =>
        (await ListFailedSinceAsync(since, cancellationToken).ConfigureAwait(false)).Count;

    public async Task<DateTimeOffset?> LastInspectedAtAsync(Guid vehicleId, CancellationToken cancellationToken) =>
        await Inspections.Where(i => i.VehicleId == new VehicleId(vehicleId))
            .MaxAsync(i => (DateTimeOffset?)i.InspectedAt, cancellationToken).ConfigureAwait(false);

    private static InspectionSummary? ToSummary(Inspection? inspection) => inspection is null ? null : Summarise(inspection);

    private static InspectionSummary Summarise(Inspection inspection) =>
        new(
            inspection.Id.Value,
            inspection.VehicleId.Value,
            inspection.InspectedAt,
            inspection.Passed,
            [.. inspection.Defects.Select(d => $"{d.Severity}: {d.Description}")],
            inspection.FollowUpWorkOrderId?.Value);
}
