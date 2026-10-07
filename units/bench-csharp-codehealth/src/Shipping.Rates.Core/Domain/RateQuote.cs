namespace Shipping.Rates.Core.Domain;

/// <summary>A priced offer from one carrier for one service level.</summary>
public sealed record RateQuote(string Carrier, ServiceLevel Level, Money Total, int TransitDays);
