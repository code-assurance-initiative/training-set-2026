namespace Shipping.Rates.Core.Carriers.Alder;

/// <summary>Alder Parcel's wire format for an address (v2 shipments API).</summary>
public sealed record AlderAddress(
    string Name,
    string? Company,
    string Street,
    string PostalCode,
    string City,
    string CountryCode);
