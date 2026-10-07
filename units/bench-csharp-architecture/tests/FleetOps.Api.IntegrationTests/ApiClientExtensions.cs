using System.Net.Http.Json;
using FleetOps.Contracts.Vehicles;
using FleetOps.Contracts.WorkOrders;

namespace FleetOps.Api.IntegrationTests;

internal static class ApiClientExtensions
{
    public static async Task<VehicleSummary> RegisterVehicleAsync(this HttpClient client, string vin, string registration, int odometerKm = 20_000)
    {
        var response = await client.PostAsJsonAsync(
            "/vehicles", new { vin, registration, model = "Transit Custom", odometerKm }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<VehicleSummary>(TestContext.Current.CancellationToken))!;
    }

    public static async Task<WorkOrderSummary> OpenWorkOrderAsync(this HttpClient client, Guid vehicleId, string title)
    {
        var response = await client.PostAsJsonAsync("/work-orders", new { vehicleId, title }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkOrderSummary>(TestContext.Current.CancellationToken))!;
    }

    public static async Task AddLineAsync(this HttpClient client, Guid workOrderId, string kind, string description, decimal quantity, decimal unitPrice)
    {
        var response = await client.PostAsJsonAsync(
            $"/work-orders/{workOrderId}/lines", new { kind, description, quantity, unitPrice }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
