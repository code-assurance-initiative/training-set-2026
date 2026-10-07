namespace ParcelTracking.Core.Tracking;

public sealed record DeliveryPerformance(string MerchantId, int Registered, int Delivered, int Failed, int Returned, double? MedianDaysToDeliver);
