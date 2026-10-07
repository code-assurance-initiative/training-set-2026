using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using ParcelTracking.Api.Contracts;
using ParcelTracking.Core.Tracking;

namespace ParcelTracking.Api.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController(TrackingService tracking, TimeProvider timeProvider) : ControllerBase
{
    [HttpGet("merchants/{merchantId}/delivery-performance")]
    public async Task<ActionResult<DeliveryPerformanceResponse>> GetDeliveryPerformance(
        string merchantId,
        [FromQuery][Range(1, 366)] int days,
        CancellationToken cancellationToken)
    {
        var to = timeProvider.GetUtcNow();
        var from = to.AddDays(-days);
        var performance = await tracking.GetDeliveryPerformanceAsync(merchantId, from, to, cancellationToken);
        return DeliveryPerformanceResponse.Create(performance, from, to);
    }
}
