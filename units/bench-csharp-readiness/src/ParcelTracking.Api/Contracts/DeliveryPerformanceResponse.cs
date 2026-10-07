using ParcelTracking.Core.Tracking;

namespace ParcelTracking.Api.Contracts;

public sealed record DeliveryPerformanceResponse(
    string MerchantId,
    DateTimeOffset From,
    DateTimeOffset To,
    int Registered,
    int Delivered,
    int Failed,
    int Returned,
    double? MedianDaysToDeliver)
{
    public static DeliveryPerformanceResponse Create(DeliveryPerformance performance, DateTimeOffset from, DateTimeOffset to)
    {
        ArgumentNullException.ThrowIfNull(performance);
        return new DeliveryPerformanceResponse(
            performance.MerchantId,
            from,
            to,
            performance.Registered,
            performance.Delivered,
            performance.Failed,
            performance.Returned,
            performance.MedianDaysToDeliver);
    }
}
