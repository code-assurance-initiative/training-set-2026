namespace ParcelTracking.Core.Parcels;

/// <summary>
/// The order in which a parcel may move through its statuses. Carriers resend and reorder scans, so a scan that
/// would move a parcel backwards is recorded as history but does not change the current status.
/// </summary>
public static class StatusTransitions
{
    public static bool IsTerminal(ParcelStatus status) =>
        status is ParcelStatus.Delivered or ParcelStatus.Returned;

    public static bool CanMove(ParcelStatus from, ParcelStatus to)
    {
        if (from == to || IsTerminal(from))
        {
            return false;
        }

        return to switch
        {
            ParcelStatus.Registered => false,
            ParcelStatus.DeliveryFailed => from is ParcelStatus.OutForDelivery,
            ParcelStatus.Returned => from is not ParcelStatus.Registered,
            _ => Rank(to) > Rank(from),
        };
    }

    private static int Rank(ParcelStatus status) => status switch
    {
        ParcelStatus.Registered => 0,
        ParcelStatus.InTransit => 1,
        ParcelStatus.DeliveryFailed => 2,
        ParcelStatus.OutForDelivery => 3,
        ParcelStatus.HeldAtPickupPoint => 4,
        ParcelStatus.Delivered => 5,
        ParcelStatus.Returned => 5,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown parcel status."),
    };
}
