namespace Shipping.Rates.Api.Contracts;

/// <summary>An address in the public quote API (v1).</summary>
public sealed record AddressDto(
    string Name,
    string? Company,
    string Street,
    string PostalCode,
    string City,
    string CountryCode);
