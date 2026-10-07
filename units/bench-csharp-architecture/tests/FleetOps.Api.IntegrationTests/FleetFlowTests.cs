using System.Net;
using System.Net.Http.Json;
using FleetOps.Contracts.Inspections;
using FleetOps.Contracts.Maintenance;
using FleetOps.Contracts.Vehicles;
using FleetOps.Contracts.WorkOrders;

namespace FleetOps.Api.IntegrationTests;

public sealed class FleetFlowTests(FleetApiFactory factory) : IClassFixture<FleetApiFactory>
{
    private readonly HttpClient _office = factory.CreateClient("fleet.read", "fleet.write");

    [Fact]
    public async Task ARegisteredVehicleCanBeReadBack()
    {
        var registered = await _office.RegisterVehicleAsync("WF0XXXTTGXKA00001", "EV11111");

        var read = await _office.GetFromJsonAsync<VehicleSummary>($"/vehicles/{registered.Id}", TestContext.Current.CancellationToken);

        Assert.Equal("EV11111", read!.Registration);
        Assert.Equal("Active", read.Status);
    }

    [Fact]
    public async Task ADuplicateVinIsAConflict()
    {
        await _office.RegisterVehicleAsync("WF0XXXTTGXKA00002", "EV22222");
        var response = await _office.PostAsJsonAsync(
            "/vehicles", new { vin = "WF0XXXTTGXKA00002", registration = "EV22223", model = "Transit", odometerKm = 1 }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AQuotedWorkOrderIsPricedAndApproved()
    {
        var vehicle = await _office.RegisterVehicleAsync("WF0XXXTTGXKA00003", "EV33333");
        var workOrder = await _office.OpenWorkOrderAsync(vehicle.Id, "Clutch replacement");
        await _office.AddLineAsync(workOrder.Id, "Labour", "Replace clutch", 10m, 0m);
        await _office.AddLineAsync(workOrder.Id, "Part", "Clutch kit", 2m, 100m);

        var response = await _office.PostAsync($"/work-orders/{workOrder.Id}/approve", null, TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var approved = await response.Content.ReadFromJsonAsync<WorkOrderSummary>(TestContext.Current.CancellationToken);
        Assert.Equal("Approved", approved!.Status);
        Assert.Equal(972.00m, approved.ApprovedTotal);
    }

    [Fact]
    public async Task ApprovingAboveTheWorkshopLimitNeedsTheManageScope()
    {
        var vehicle = await _office.RegisterVehicleAsync("WF0XXXTTGXKA00004", "EV44444");
        var workOrder = await _office.OpenWorkOrderAsync(vehicle.Id, "Gearbox");
        await _office.AddLineAsync(workOrder.Id, "Part", "Gearbox", 1m, 3_000m);

        var refused = await _office.PostAsync($"/work-orders/{workOrder.Id}/approve", null, TestContext.Current.CancellationToken);
        var manager = factory.CreateClient("fleet.read", "fleet.write", "fleet.manage");
        var accepted = await manager.PostAsync($"/work-orders/{workOrder.Id}/approve", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
    }

    [Fact]
    public async Task AFailedInspectionOpensARepairWorkOrder()
    {
        var vehicle = await _office.RegisterVehicleAsync("WF0XXXTTGXKA00005", "EV55555");

        var response = await _office.PostAsJsonAsync(
            "/inspections",
            new { vehicleId = vehicle.Id, defects = new[] { new { description = "Tyre cord exposed", severity = "Dangerous" } } },
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var inspection = await response.Content.ReadFromJsonAsync<InspectionSummary>(TestContext.Current.CancellationToken);
        Assert.False(inspection!.Passed);
        var repair = await _office.GetFromJsonAsync<WorkOrderSummary>($"/work-orders/{inspection.FollowUpWorkOrderId}", TestContext.Current.CancellationToken);
        Assert.Equal("Repair: Tyre cord exposed", repair!.Title);
    }

    [Fact]
    public async Task DueMaintenanceUsesTheLiveOdometer()
    {
        var vehicle = await _office.RegisterVehicleAsync("WF0XXXTTGXKA00006", "EV66666", odometerKm: 1_000);
        factory.Telematics.Odometer = 15_100;

        var due = await _office.GetFromJsonAsync<List<MaintenanceDue>>("/maintenance/due", TestContext.Current.CancellationToken);

        Assert.Contains(due!, d => d.VehicleId == vehicle.Id && d.Service == "Oil and filters" && d.CurrentKm == 15_100);
    }

    [Fact]
    public async Task TheDashboardCountsTheFleet()
    {
        var vehicle = await _office.RegisterVehicleAsync("WF0XXXTTGXKA00007", "EV77777");
        await _office.OpenWorkOrderAsync(vehicle.Id, "Service");

        var dashboard = await _office.GetFromJsonAsync<Dictionary<string, int>>("/dashboard", TestContext.Current.CancellationToken);

        Assert.True(dashboard!["inWorkshop"] >= 1);
        Assert.True(dashboard["openWorkOrders"] >= 1);
    }
}
