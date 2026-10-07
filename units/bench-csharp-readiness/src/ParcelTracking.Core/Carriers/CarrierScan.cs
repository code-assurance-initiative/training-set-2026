namespace ParcelTracking.Core.Carriers;

/// <summary>One scan of a parcel as the carrier reports it, before normalisation.</summary>
public sealed record CarrierScan(string StatusCode, DateTimeOffset OccurredAt, string? Location);
