using FleetOps.Infrastructure.Documents;

namespace FleetOps.Infrastructure.Reporting;

/// <summary>Every report the fleet office asks for.</summary>
public interface IFleetReporting
{
    Task<IReadOnlyList<CostPerVehicleRow>> CostPerVehicleAsync(ReportPeriod period, CancellationToken cancellationToken);

    Task<IReadOnlyList<CostBreakdownRow>> CostBreakdownAsync(ReportPeriod period, CancellationToken cancellationToken);

    Task<decimal> TotalApprovedCostAsync(ReportPeriod period, CancellationToken cancellationToken);

    Task<IReadOnlyList<DowntimeRow>> DowntimeAsync(DateTimeOffset asOf, CancellationToken cancellationToken);

    Task<IReadOnlyList<FuelEfficiencyRow>> FuelByVehicleAsync(ReportPeriod period, CancellationToken cancellationToken);

    Task<IReadOnlyList<FuelStationRow>> FuelByStationAsync(ReportPeriod period, CancellationToken cancellationToken);

    Task<decimal> TotalFuelSpendAsync(ReportPeriod period, CancellationToken cancellationToken);

    Task<InspectionPassRateRow> InspectionPassRateAsync(ReportPeriod period, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> MostCommonDefectsAsync(ReportPeriod period, int top, CancellationToken cancellationToken);

    Task<IReadOnlyList<OpenWorkOrderAgeRow>> OpenWorkOrderAgesAsync(DateTimeOffset asOf, CancellationToken cancellationToken);

    Task<int> WorkOrdersAwaitingApprovalAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<VehicleUtilisationRow>> UtilisationAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<VehicleUtilisationRow>> OverdueForServiceAsync(int toleranceKm, CancellationToken cancellationToken);

    Task<int> ActiveVehicleCountAsync(CancellationToken cancellationToken);

    Task<MonthlyFleetReport> MonthlyReportAsync(int year, int month, CancellationToken cancellationToken);

    Task<ExportedDocument> ExportAsync(ReportExportRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> AvailableReportsAsync(CancellationToken cancellationToken);
}
