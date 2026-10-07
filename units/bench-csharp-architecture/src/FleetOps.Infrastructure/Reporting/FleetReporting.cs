using System.Globalization;
using FleetOps.Domain.Maintenance;
using FleetOps.Domain.Vehicles;
using FleetOps.Domain.WorkOrders;
using FleetOps.Infrastructure.Documents;
using FleetOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Infrastructure.Reporting;

public sealed class FleetReporting(FleetOpsDbContext db, DocumentNameBuilder names) : IFleetReporting
{
    private static readonly string[] Reports = ["cost-per-vehicle", "downtime", "fuel-by-vehicle", "open-work-orders"];

    public async Task<IReadOnlyList<CostPerVehicleRow>> CostPerVehicleAsync(ReportPeriod period, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(period);
        var approved = await ApprovedIn(period).ToListAsync(cancellationToken).ConfigureAwait(false);
        var vehicles = await db.Vehicles.AsNoTracking().ToDictionaryAsync(v => v.Id, v => v.Registration, cancellationToken).ConfigureAwait(false);
        return
        [
            .. approved
                .GroupBy(w => w.VehicleId)
                .Select(g => new CostPerVehicleRow(g.Key.Value, vehicles.GetValueOrDefault(g.Key, "?"), g.Sum(w => w.ApprovedTotal ?? 0m)))
                .OrderByDescending(r => r.ApprovedCost),
        ];
    }

    public async Task<IReadOnlyList<CostBreakdownRow>> CostBreakdownAsync(ReportPeriod period, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(period);
        var approved = await ApprovedIn(period).ToListAsync(cancellationToken).ConfigureAwait(false);
        return
        [
            .. approved
                .SelectMany(w => w.Lines)
                .GroupBy(l => l.Kind)
                .Select(g => new CostBreakdownRow(g.Key.ToString(), g.Sum(l => l.Quantity * l.UnitPrice))),
        ];
    }

    public async Task<decimal> TotalApprovedCostAsync(ReportPeriod period, CancellationToken cancellationToken) =>
        (await CostPerVehicleAsync(period, cancellationToken).ConfigureAwait(false)).Sum(r => r.ApprovedCost);

    public async Task<IReadOnlyList<DowntimeRow>> DowntimeAsync(DateTimeOffset asOf, CancellationToken cancellationToken)
    {
        var open = await db.WorkOrders.AsNoTracking().Where(w => w.Status != WorkOrderStatus.Completed)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var vehicles = await db.Vehicles.AsNoTracking().Where(v => v.Status == VehicleStatus.InWorkshop)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return
        [
            .. vehicles.Select(v => new DowntimeRow(
                v.Id.Value,
                v.Registration,
                open.Where(w => w.VehicleId == v.Id).Select(w => (int)(asOf - w.OpenedAt).TotalDays).DefaultIfEmpty(0).Max())),
        ];
    }

    public async Task<IReadOnlyList<FuelEfficiencyRow>> FuelByVehicleAsync(ReportPeriod period, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(period);
        var fuel = await db.FuelTransactions.AsNoTracking()
            .Where(t => t.PurchasedAt >= period.From && t.PurchasedAt < period.To)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. fuel.GroupBy(t => t.Vin).Select(g => new FuelEfficiencyRow(g.Key, g.Sum(t => t.Litres), g.Sum(t => t.Amount)))];
    }

    public async Task<IReadOnlyList<FuelStationRow>> FuelByStationAsync(ReportPeriod period, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(period);
        var fuel = await db.FuelTransactions.AsNoTracking()
            .Where(t => t.PurchasedAt >= period.From && t.PurchasedAt < period.To && t.Litres > 0)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return
        [
            .. fuel.GroupBy(t => t.Station)
                .Select(g => new FuelStationRow(g.Key, g.Sum(t => t.Litres), Math.Round(g.Sum(t => t.Amount) / g.Sum(t => t.Litres), 3))),
        ];
    }

    public async Task<decimal> TotalFuelSpendAsync(ReportPeriod period, CancellationToken cancellationToken) =>
        (await FuelByVehicleAsync(period, cancellationToken).ConfigureAwait(false)).Sum(r => r.Amount);

    public async Task<InspectionPassRateRow> InspectionPassRateAsync(ReportPeriod period, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(period);
        var inspections = await db.Inspections.AsNoTracking()
            .Where(i => i.InspectedAt >= period.From && i.InspectedAt < period.To)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var failed = inspections.Count(i => !i.Passed);
        return new InspectionPassRateRow(inspections.Count, failed, inspections.Count == 0 ? 1.0 : 1.0 - ((double)failed / inspections.Count));
    }

    public async Task<IReadOnlyList<string>> MostCommonDefectsAsync(ReportPeriod period, int top, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(period);
        var inspections = await db.Inspections.AsNoTracking()
            .Where(i => i.InspectedAt >= period.From && i.InspectedAt < period.To)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return
        [
            .. inspections.SelectMany(i => i.Defects)
                .GroupBy(d => d.Description, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Take(top)
                .Select(g => g.Key),
        ];
    }

    public async Task<IReadOnlyList<OpenWorkOrderAgeRow>> OpenWorkOrderAgesAsync(DateTimeOffset asOf, CancellationToken cancellationToken)
    {
        var open = await db.WorkOrders.AsNoTracking().Where(w => w.Status != WorkOrderStatus.Completed)
            .OrderBy(w => w.OpenedAt).ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. open.Select(w => new OpenWorkOrderAgeRow(w.Id.Value, w.Title, (int)(asOf - w.OpenedAt).TotalDays))];
    }

    public Task<int> WorkOrdersAwaitingApprovalAsync(CancellationToken cancellationToken) =>
        db.WorkOrders.CountAsync(w => w.Status == WorkOrderStatus.Quoted, cancellationToken);

    public async Task<IReadOnlyList<VehicleUtilisationRow>> UtilisationAsync(CancellationToken cancellationToken)
    {
        var vehicles = await db.Vehicles.AsNoTracking().Where(v => v.Status != VehicleStatus.Retired)
            .OrderBy(v => v.Registration).ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. vehicles.Select(v => new VehicleUtilisationRow(v.Id.Value, v.Registration, v.OdometerKm, v.OdometerKm - v.LastServiceKm))];
    }

    public async Task<IReadOnlyList<VehicleUtilisationRow>> OverdueForServiceAsync(int toleranceKm, CancellationToken cancellationToken)
    {
        var interval = MaintenancePlan.Standard.Intervals.Min(i => i.EveryKm);
        return [.. (await UtilisationAsync(cancellationToken).ConfigureAwait(false)).Where(r => r.KmSinceService > interval + toleranceKm)];
    }

    public Task<int> ActiveVehicleCountAsync(CancellationToken cancellationToken) =>
        db.Vehicles.CountAsync(v => v.Status != VehicleStatus.Retired, cancellationToken);

    public async Task<MonthlyFleetReport> MonthlyReportAsync(int year, int month, CancellationToken cancellationToken)
    {
        var from = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        var period = new ReportPeriod(from, from.AddMonths(1));
        return new MonthlyFleetReport(
            period,
            await CostPerVehicleAsync(period, cancellationToken).ConfigureAwait(false),
            await InspectionPassRateAsync(period, cancellationToken).ConfigureAwait(false),
            await OpenWorkOrderAgesAsync(period.To, cancellationToken).ConfigureAwait(false));
    }

    public async Task<ExportedDocument> ExportAsync(ReportExportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var options = new CsvExportOptions();
        var content = request.ReportName switch
        {
            "cost-per-vehicle" => CsvDocument.Write(
                await CostPerVehicleAsync(request.Period, cancellationToken).ConfigureAwait(false),
                [new CsvColumn<CostPerVehicleRow>("Registration", r => r.Registration),
                 new CsvColumn<CostPerVehicleRow>("Approved cost", r => r.ApprovedCost.ToString("0.00", CultureInfo.InvariantCulture))],
                options),
            "open-work-orders" => CsvDocument.Write(
                await OpenWorkOrderAgesAsync(request.Period.To, cancellationToken).ConfigureAwait(false),
                [new CsvColumn<OpenWorkOrderAgeRow>("Title", r => r.Title),
                 new CsvColumn<OpenWorkOrderAgeRow>("Age (days)", r => r.AgeDays.ToString(CultureInfo.InvariantCulture))],
                options),
            _ => throw new ArgumentException($"Unknown report '{request.ReportName}'.", nameof(request)),
        };
        return new ExportedDocument(names.Build(request.ReportName, ExportFormat.Csv), "text/csv", content);
    }

    public Task<IReadOnlyList<string>> AvailableReportsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>(Reports);

    private IQueryable<WorkOrder> ApprovedIn(ReportPeriod period) =>
        db.WorkOrders.AsNoTracking().Where(w => w.ApprovedTotal != null && w.OpenedAt >= period.From && w.OpenedAt < period.To);
}
