using System.Net.Http.Json;
using ParcelTracking.Api.Contracts;

namespace ParcelTracking.IntegrationTests;

public sealed class ReportsEndpointsTests(TrackingApiFactory factory) : IClassFixture<TrackingApiFactory>
{
    [Fact]
    public async Task Delivery_performance_counts_the_merchants_parcels()
    {
        var writer = factory.CreateClient("merchant-r", ApiTestData.Read, ApiTestData.Write);
        await ApiTestData.RegisterAsync(writer, TestContext.Current.CancellationToken);
        await ApiTestData.RegisterAsync(writer, TestContext.Current.CancellationToken);
        var reporter = factory.CreateClient("merchant-r", ApiTestData.Reports);

        var report = await reporter.GetFromJsonAsync<DeliveryPerformanceResponse>(
            "/api/reports/merchants/merchant-r/delivery-performance?days=30",
            TestContext.Current.CancellationToken);

        Assert.NotNull(report);
        Assert.Equal(2, report.Registered);
        Assert.Equal(0, report.Delivered);
        Assert.Null(report.MedianDaysToDeliver);
    }
}
