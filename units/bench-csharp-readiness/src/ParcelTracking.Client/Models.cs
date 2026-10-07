namespace ParcelTracking.Client;

/// <summary>A parcel as the API returns it.</summary>
/// <param name="TrackingNumber">The carrier's tracking number.</param>
/// <param name="CarrierCode">The carrier, e.g. <c>NORDPOST</c>.</param>
/// <param name="CarrierName">The carrier's display name, when known.</param>
/// <param name="Status">The current status, e.g. <c>InTransit</c>.</param>
/// <param name="RegisteredAt">When the shipment was registered.</param>
/// <param name="DeliveredAt">When it was delivered, if it has been.</param>
/// <param name="PickupPointId">The pickup point it was redirected to, if any.</param>
/// <param name="Events">The carrier scans, oldest first.</param>
public sealed record ParcelView(
    string TrackingNumber,
    string CarrierCode,
    string? CarrierName,
    string Status,
    DateTimeOffset RegisteredAt,
    DateTimeOffset? DeliveredAt,
    string? PickupPointId,
    IReadOnlyList<TrackingEventView> Events);

/// <summary>One carrier scan.</summary>
/// <param name="OccurredAt">When the carrier scanned the parcel.</param>
/// <param name="Status">The status the scan moved the parcel to.</param>
/// <param name="Location">Where it was scanned, when the carrier says.</param>
public sealed record TrackingEventView(DateTimeOffset OccurredAt, string Status, string? Location);

/// <summary>A new shipment to register.</summary>
/// <param name="TrackingNumber">The carrier's tracking number (upper-case letters and digits).</param>
/// <param name="CarrierCode">The carrier code.</param>
/// <param name="DestinationPostalCode">The destination postal code.</param>
public sealed record ShipmentRegistration(string TrackingNumber, string CarrierCode, string DestinationPostalCode);
