namespace Fx.Conversion.Api.Contracts;

public sealed record RatesResponse(string Base, DateOnly PublishedOn, IReadOnlyDictionary<string, decimal> Rates);
