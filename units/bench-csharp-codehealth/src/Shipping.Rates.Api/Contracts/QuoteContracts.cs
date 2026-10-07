using Shipping.Rates.Core.Domain;

namespace Shipping.Rates.Api.Contracts;

public sealed record ParcelDto(int WeightGrams, int LengthCm, int WidthCm, int HeightCm, bool DangerousGoods = false);

public sealed record QuoteRequestDto(
    AddressDto? Sender,
    AddressDto? Recipient,
    IReadOnlyList<ParcelDto>? Parcels,
    ServiceLevel Level = ServiceLevel.Standard,
    DateOnly? ShipDate = null,
    decimal InsuredValue = 0m,
    decimal? MaxPrice = null);

public sealed record QuoteResponseDto(string Carrier, string CarrierName, ServiceLevel Level, decimal Price, string Currency, int TransitDays);

/// <summary>Turns the wire contract into the domain request, collecting every problem instead of stopping at the first.</summary>
public static class QuoteRequestMapper
{
    private const int MaxParcels = 20;

    public static bool TryMap(QuoteRequestDto dto, DateOnly today, out QuoteRequest? request, out Dictionary<string, string[]> errors)
    {
        ArgumentNullException.ThrowIfNull(dto);
        errors = [];
        request = null;
        var sender = MapAddress(dto.Sender, "sender", errors);
        var recipient = MapAddress(dto.Recipient, "recipient", errors);
        if (dto.Parcels is null || dto.Parcels.Count is 0 or > MaxParcels)
        {
            errors["parcels"] = [$"Between 1 and {MaxParcels} parcels are required."];
        }
        else if (dto.Parcels.Any(p => p.WeightGrams <= 0 || p.LengthCm <= 0 || p.WidthCm <= 0 || p.HeightCm <= 0))
        {
            errors["parcels"] = ["Weights and dimensions must be positive."];
        }

        if (dto.InsuredValue < 0)
        {
            errors["insuredValue"] = ["Must not be negative."];
        }

        if (errors.Count > 0 || sender is null || recipient is null || dto.Parcels is null)
        {
            return false;
        }

        var parcels = dto.Parcels.Select(p => new Parcel(p.WeightGrams, p.LengthCm, p.WidthCm, p.HeightCm, p.DangerousGoods)).ToList();
        request = new QuoteRequest(sender, recipient, parcels, dto.Level, dto.ShipDate ?? today, dto.InsuredValue, dto.MaxPrice);
        return true;
    }

    private static Address? MapAddress(AddressDto? dto, string field, Dictionary<string, string[]> errors)
    {
        if (dto is null)
        {
            errors[field] = ["Required."];
            return null;
        }

        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            problems.Add("name is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.PostalCode))
        {
            problems.Add("postalCode is required.");
        }

        if (dto.CountryCode is not { Length: 2 } || !dto.CountryCode.All(char.IsAsciiLetterUpper))
        {
            problems.Add("countryCode must be an ISO 3166-1 alpha-2 code in upper case.");
        }

        if (problems.Count > 0)
        {
            errors[field] = [.. problems];
            return null;
        }

        return new Address(dto.Name, dto.Company, dto.Street, dto.PostalCode, dto.City, dto.CountryCode);
    }
}
