namespace Shipping.Rates.Core.Pricing;

/// <summary>One carrier's published tariff: a fixed fee plus a price per chargeable kilogram per zone.</summary>
public sealed record RateCard(
    string Carrier,
    string Currency,
    decimal BaseFee,
    IReadOnlyDictionary<int, decimal> PerKgByZone,
    int VolumetricDivisor,
    DateTimeOffset FetchedAt)
{
    public decimal PerKgFor(int zone) =>
        PerKgByZone.TryGetValue(zone, out var price)
            ? price
            : throw new InvalidOperationException($"Rate card for {Carrier} has no price for zone {zone}.");
}

/// <summary>Supplies the current rate card of a carrier.</summary>
public interface IRateCardProvider
{
    RateCard GetCard(string carrier);
}

public sealed class RateCardUnavailableException(string carrier)
    : Exception($"No current rate card is available for {carrier}.")
{
    public string Carrier { get; } = carrier;
}
