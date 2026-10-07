namespace Shipping.Rates.Core.Domain;

/// <summary>A postal address. <see cref="CountryCode"/> is ISO 3166-1 alpha-2.</summary>
public sealed record Address(
    string Name,
    string? Company,
    string Street,
    string PostalCode,
    string City,
    string CountryCode);
