using FleetOps.Application.Abstractions;
using FleetOps.Application.Features.Vehicles;
using FleetOps.Application.Tests.Support;
using FleetOps.Domain.Inspections;
using FleetOps.Domain.Vehicles;
using FleetOps.Domain.WorkOrders;
using FleetOps.Infrastructure.Auditing;
using FleetOps.Infrastructure.Documents;
using FleetOps.Infrastructure.FuelCards;
using FleetOps.Infrastructure.Persistence;
using FleetOps.Infrastructure.Reporting;
using Microsoft.Extensions.Options;

namespace FleetOps.Application.Tests.Infrastructure;

/// <summary>The read model, reports and audit trail over a seeded database: two vans, one work order, two inspections.</summary>
public sealed class ReadSideTests : IDisposable
{
    private static readonly DateTimeOffset May = new(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
    private readonly TestDatabase _database = new();
    private readonly Guid _van1;
    private readonly Guid _van2;
    private readonly Guid _workOrder;
    private readonly Guid _failedInspection;

    public ReadSideTests()
    {
        using var db = _database.CreateContext();
        var van1 = Vehicle.Register(new Vin("WVWZZZ1KZAW000001"), "AB12345", "Transit", 40_000);
        var van2 = Vehicle.Register(new Vin("WVWZZZ1KZAW000002"), "CD67890", "Sprinter", 10_000);
        van1.RecordOdometer(60_000);
        van2.SendToWorkshop();
        var order = WorkOrder.Open(van2.Id, "Brakes", May.AddDays(2));
        order.AddLine(new WorkOrderLine(LineKind.Labour, "Fit pads", 2m, 68m));
        order.AddLine(new WorkOrderLine(LineKind.Part, "Pads", 1m, 90m));
        order.Approve(236m);
        var failed = Inspection.Record(van2.Id, May.AddDays(1), [new Defect("Brake line leak", DefectSeverity.Dangerous)]);
        var passed = Inspection.Record(van1.Id, May.AddDays(3), [new Defect("Wiper worn", DefectSeverity.Minor)]);
        db.AddRange(van1, van2, order, failed, passed);
        db.FuelTransactions.AddRange(
            new FuelTransaction { Id = Guid.NewGuid(), ProviderReference = "F1", Vin = van1.Vin.Value, Station = "Køge", Litres = 50m, Amount = 700m, PurchasedAt = May.AddDays(4) },
            new FuelTransaction { Id = Guid.NewGuid(), ProviderReference = "F2", Vin = van1.Vin.Value, Station = "Køge", Litres = 40m, Amount = 580m, PurchasedAt = May.AddDays(5) });
        db.SaveChanges();
        (_van1, _van2, _workOrder, _failedInspection) = (van1.Id.Value, van2.Id.Value, order.Id.Value, failed.Id.Value);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static ReportPeriod MayPeriod => new(May, May.AddMonths(1));

    [Fact]
    public async Task VehicleQueries()
    {
        await using var db = _database.CreateContext();
        var read = new FleetReadModel(db);

        Assert.Equal("AB12345", (await read.GetVehicleAsync(_van1, Ct))!.Registration);
        Assert.Equal(2, (await read.ListVehiclesAsync(1, 10, Ct)).TotalCount);
        Assert.Equal(1, await read.CountActiveVehiclesAsync(Ct));
        Assert.Equal(1, await read.CountVehiclesInWorkshopAsync(Ct));
        Assert.Equal("CD67890", Assert.Single(await read.FindByRegistrationAsync("cd", Ct)).Registration);
        Assert.Equal(_van1, Assert.Single(await read.ListOverdueForServiceAsync(1_000, Ct)).Id);
        Assert.Single((await new ListVehiclesHandler(read).Handle(new ListVehiclesQuery(2, 1), Ct)).Items);
    }

    [Fact]
    public async Task WorkOrderAndInspectionQueries()
    {
        await using var db = _database.CreateContext();
        var read = new FleetReadModel(db);
        IInspectionReadModel inspections = read;

        Assert.Equal("Brakes", (await read.GetWorkOrderAsync(_workOrder, Ct))!.Title);
        Assert.Single(await read.ListForVehicleAsync(_van2, Ct));
        Assert.Single(await read.ListOpenAsync(Ct));
        Assert.Equal(1, await read.CountOpenAsync(Ct));
        Assert.Equal(0, await read.CountAwaitingApprovalAsync(Ct));
        Assert.False((await inspections.GetInspectionAsync(_failedInspection, Ct))!.Passed);
        Assert.Single(await inspections.ListForVehicleAsync(_van1, Ct));
        Assert.Single(await inspections.ListFailedSinceAsync(May, Ct));
        Assert.Equal(2, await inspections.CountSinceAsync(May, Ct));
        Assert.Equal(1, await inspections.CountFailedSinceAsync(May, Ct));
        Assert.Equal(May.AddDays(3), await inspections.LastInspectedAtAsync(_van1, Ct));
    }

    [Fact]
    public async Task CostAndFuelReports()
    {
        await using var db = _database.CreateContext();
        var reporting = Reporting(db);

        Assert.Equal(236m, Assert.Single(await reporting.CostPerVehicleAsync(MayPeriod, Ct)).ApprovedCost);
        Assert.Equal(2, (await reporting.CostBreakdownAsync(MayPeriod, Ct)).Count);
        Assert.Equal(236m, await reporting.TotalApprovedCostAsync(MayPeriod, Ct));
        Assert.Equal(90m, Assert.Single(await reporting.FuelByVehicleAsync(MayPeriod, Ct)).Litres);
        Assert.Equal(14.222m, Assert.Single(await reporting.FuelByStationAsync(MayPeriod, Ct)).AveragePricePerLitre);
        Assert.Equal(1_280m, await reporting.TotalFuelSpendAsync(MayPeriod, Ct));
    }

    [Fact]
    public async Task FleetAndInspectionReports()
    {
        await using var db = _database.CreateContext();
        var reporting = Reporting(db);

        Assert.Equal(5, Assert.Single(await reporting.DowntimeAsync(May.AddDays(7), Ct)).DaysInWorkshop);
        Assert.Equal(0.5, (await reporting.InspectionPassRateAsync(MayPeriod, Ct)).PassRate);
        Assert.Equal("Brake line leak", (await reporting.MostCommonDefectsAsync(MayPeriod, 1, Ct))[0]);
        Assert.Equal(5, Assert.Single(await reporting.OpenWorkOrderAgesAsync(May.AddDays(7), Ct)).AgeDays);
        Assert.Equal(0, await reporting.WorkOrdersAwaitingApprovalAsync(Ct));
        Assert.Equal(2, (await reporting.UtilisationAsync(Ct)).Count);
        Assert.Single(await reporting.OverdueForServiceAsync(0, Ct));
        Assert.Equal(2, await reporting.ActiveVehicleCountAsync(Ct));
        Assert.Single((await reporting.MonthlyReportAsync(2026, 5, Ct)).Costs);
        Assert.Contains("cost-per-vehicle", await reporting.AvailableReportsAsync(Ct));
    }

    [Fact]
    public async Task ReportsExportAsCsv()
    {
        await using var db = _database.CreateContext();
        var reporting = Reporting(db);

        var costs = await reporting.ExportAsync(new ReportExportRequest("cost-per-vehicle", MayPeriod, ReportFormat.Csv), Ct);
        var open = await reporting.ExportAsync(new ReportExportRequest("open-work-orders", MayPeriod, ReportFormat.Csv), Ct);

        Assert.Equal("Registration,Approved cost\r\nCD67890,236.00\r\n", costs.Content);
        Assert.StartsWith("fleetops-cost-per-vehicle-", costs.FileName, StringComparison.Ordinal);
        Assert.Contains("Brakes", open.Content, StringComparison.Ordinal);
        await Assert.ThrowsAsync<ArgumentException>(() => reporting.ExportAsync(new ReportExportRequest("nope", MayPeriod, ReportFormat.Csv), Ct));
    }

    [Fact]
    public async Task EveryChangeIsAuditedAndOldEntriesArePurged()
    {
        await using var db = _database.CreateContext();
        var trail = new AuditTrail(db, Options.Create(new AuditOptions { Retention = new AuditRetentionPolicy { KeepDays = 0 } }), _database.Clock);

        var history = await trail.HistoryAsync(new AuditQuery("Vehicle", _van1.ToString()), Ct);
        Assert.Equal(AuditAction.Created, Assert.Single(history).Action);

        _database.Clock.Advance(TimeSpan.FromDays(1));
        Assert.True(await trail.PurgeExpiredAsync(Ct) >= 5);
    }

    [Fact]
    public async Task FuelImportSkipsKnownTransactionsAndPricesAreCached()
    {
        await using var db = _database.CreateContext();
        var store = new FuelCardTransactionStore(db);
        var client = new PagedFuelCards();

        var result = await new FuelCardImportService(client, store).ImportAsync(Ct);
        var cache = new FuelPriceCache(store, _database.Clock);
        var price = await cache.GetAsync("Køge", Ct);

        Assert.Equal(new FuelCardImportResult(3, 1), result);
        Assert.Equal(14.5m, price!.PricePerLitre);
        Assert.Same(price.Station, (await cache.GetAsync("Køge", Ct))!.Station);
        Assert.Null(await cache.GetAsync("Roskilde", Ct));
    }

    public void Dispose() => _database.Dispose();

    private FleetReporting Reporting(FleetOpsDbContext db) =>
        new(db, new DocumentNameBuilder(Options.Create(new DocumentOptions()), _database.Clock));

    private sealed class PagedFuelCards : IFuelCardClient
    {
        public Task<FuelCardTransactionPage> GetTransactionsAsync(string? cursor, CancellationToken cancellationToken) =>
            Task.FromResult(cursor is null
                ? new FuelCardTransactionPage(
                    [new FuelTransactionDto("F1", "V", "Køge", 50m, 700m, May), new FuelTransactionDto("F2", "V", "Køge", 40m, 580m, May)], "next")
                : new FuelCardTransactionPage([new FuelTransactionDto("F3", "V", "Køge", 10m, 145m, May.AddDays(9))], null));

        public Task<FuelCardAccount> GetAccountAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
