namespace Shipping.Rates.Core.Domain;

/// <summary>What the caller wants shipped, from where to where, and how fast.</summary>
public sealed record QuoteRequest(
    Address Sender,
    Address Recipient,
    IReadOnlyList<Parcel> Parcels,
    ServiceLevel Level,
    DateOnly ShipDate,
    decimal InsuredValue = 0m,
    decimal? MaxPrice = null);
