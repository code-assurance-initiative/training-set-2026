namespace ParcelTracking.Core.Parcels;

public enum ParcelStatus
{
    Registered = 0,
    InTransit = 1,
    OutForDelivery = 2,
    HeldAtPickupPoint = 3,
    Delivered = 4,
    DeliveryFailed = 5,
    Returned = 6,
}
