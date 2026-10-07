using Shipping.Rates.Core.Domain;

namespace Shipping.Rates.Core.Carriers;

public sealed record CarrierRate(string Carrier, ServiceLevel Level, decimal Amount, string Currency, int TransitDays);

public sealed record CarrierLabel(string Carrier, string TrackingNumber, string Zpl);

public sealed record CarrierCapabilities(bool SupportsInsurance, bool SupportsPickup, int MaxWeightGrams);

public enum TrackingStatus
{
    Unknown,
    AwaitingCollection,
    InTransit,
    OutForDelivery,
    Delivered,
    Returned,
}

public sealed class CarrierException(string carrier, string errorCode)
    : Exception($"Carrier {carrier} rejected the request ({errorCode}).")
{
    public string Carrier { get; } = carrier;

    public string ErrorCode { get; } = errorCode;
}

public sealed class UnknownCarrierException(string code) : Exception($"No carrier adapter is registered for '{code}'.")
{
    public string Code { get; } = code;
}
