using FleetOps.Application.Features.Inspections;
using FleetOps.Application.Features.Maintenance;
using FleetOps.Application.Features.WorkOrders;
using FleetOps.Application.Tests.Support;
using FleetOps.Domain.Inspections;
using FleetOps.Domain.Maintenance;
using FleetOps.Domain.Vehicles;
using FleetOps.Domain.WorkOrders;
using FleetOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Application.Tests.Features;

public sealed class WorkOrderHandlerTests : IDisposable
{
    private readonly TestDatabase _database = new();

    [Fact]
    public async Task OpeningAWorkOrderSendsTheVehicleToTheWorkshop()
    {
        var vehicleId = await SeedVehicleAsync();
        await using var db = _database.CreateContext();

        var summary = await OpenHandler(db).Handle(new OpenWorkOrderCommand(vehicleId, "Replace clutch"), TestContext.Current.CancellationToken);

        Assert.Equal("Open", summary.Status);
        Assert.Equal(VehicleStatus.InWorkshop, (await db.Vehicles.SingleAsync(TestContext.Current.CancellationToken)).Status);
    }

    [Fact]
    public async Task ADangerousDefectOpensAFollowUpWorkOrder()
    {
        var vehicleId = await SeedVehicleAsync();
        await using var db = _database.CreateContext();
        var handler = new RecordInspectionHandler(new InspectionRepository(db), db, _database.Clock, OpenHandler(db));

        var summary = await handler.Handle(
            new RecordInspectionCommand(vehicleId, [new Defect("Brake line leak", DefectSeverity.Dangerous), new Defect("Wiper worn", DefectSeverity.Minor)]),
            TestContext.Current.CancellationToken);

        Assert.False(summary.Passed);
        Assert.NotNull(summary.FollowUpWorkOrderId);
        var workOrder = await db.WorkOrders.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Repair: Brake line leak", workOrder.Title);
    }

    [Fact]
    public async Task APassedInspectionOpensNothing()
    {
        var vehicleId = await SeedVehicleAsync();
        await using var db = _database.CreateContext();
        var handler = new RecordInspectionHandler(new InspectionRepository(db), db, _database.Clock, OpenHandler(db));

        var summary = await handler.Handle(
            new RecordInspectionCommand(vehicleId, [new Defect("Wiper worn", DefectSeverity.Advisory)]), TestContext.Current.CancellationToken);

        Assert.True(summary.Passed);
        Assert.Empty(db.WorkOrders);
    }

    [Fact]
    public async Task LinesAndApprovalGoThroughTheDomainRules()
    {
        var vehicleId = await SeedVehicleAsync();
        await using var db = _database.CreateContext();
        var opened = await OpenHandler(db).Handle(new OpenWorkOrderCommand(vehicleId, "Service"), TestContext.Current.CancellationToken);
        var repository = new WorkOrderRepository(db);

        await new AddWorkOrderLineHandler(repository, db)
            .Handle(new AddWorkOrderLineCommand(opened.Id, LineKind.Labour, "Service", 2m, 68m), TestContext.Current.CancellationToken);
        var approved = await new ApproveWorkOrderHandler(repository, db)
            .Handle(new ApproveWorkOrderCommand(opened.Id, 136m), TestContext.Current.CancellationToken);

        Assert.Equal("Approved", approved.Status);
        Assert.Equal(136m, approved.ApprovedTotal);
    }

    [Fact]
    public async Task DueMaintenanceIsJudgedOnTheLiveOdometer()
    {
        var vehicleId = await SeedVehicleAsync(odometer: 14_000);
        await using var db = _database.CreateContext();
        var telematics = new FakeTelematics { Odometers = { ["WVWZZZ1KZAW000001"] = 15_200 } };
        var handler = new GetDueMaintenanceHandler(new VehicleRepository(db), new MaintenanceDueEvaluator(telematics));

        var due = await handler.Handle(new GetDueMaintenanceQuery(), TestContext.Current.CancellationToken);

        var item = Assert.Single(due);
        Assert.Equal(vehicleId, item.VehicleId);
        Assert.Equal("Oil and filters", item.Service);
        Assert.Equal(15_200, item.CurrentKm);
    }

    public void Dispose() => _database.Dispose();

    private OpenWorkOrderHandler OpenHandler(FleetOpsDbContext db) =>
        new(new VehicleRepository(db), new WorkOrderRepository(db), db, _database.Clock);

    private async Task<Guid> SeedVehicleAsync(int odometer = 20_000)
    {
        await using var db = _database.CreateContext();
        var vehicle = Vehicle.Register(new Vin("WVWZZZ1KZAW000001"), "AB12345", "Transit", odometer);
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return vehicle.Id.Value;
    }
}
