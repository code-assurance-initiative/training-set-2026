namespace FleetOps.Infrastructure.Reporting;

public enum ReportFormat
{
    Json,
    Csv,
}

public sealed record ReportPeriod(DateTimeOffset From, DateTimeOffset To);

public sealed record CostPerVehicleRow(Guid VehicleId, string Registration, decimal ApprovedCost);

public sealed record CostBreakdownRow(string Category, decimal Amount);

public sealed record DowntimeRow(Guid VehicleId, string Registration, int DaysInWorkshop);

public sealed record FuelEfficiencyRow(string Vin, decimal Litres, decimal Amount);

public sealed record FuelStationRow(string Station, decimal Litres, decimal AveragePricePerLitre);

public sealed record InspectionPassRateRow(int Inspections, int Failed, double PassRate);

public sealed record OpenWorkOrderAgeRow(Guid WorkOrderId, string Title, int AgeDays);

public sealed record VehicleUtilisationRow(Guid VehicleId, string Registration, int OdometerKm, int KmSinceService);

public sealed record MonthlyFleetReport(
    ReportPeriod Period,
    IReadOnlyList<CostPerVehicleRow> Costs,
    InspectionPassRateRow Inspections,
    IReadOnlyList<OpenWorkOrderAgeRow> OpenWorkOrders);

public sealed record ReportExportRequest(string ReportName, ReportPeriod Period, ReportFormat Format);
