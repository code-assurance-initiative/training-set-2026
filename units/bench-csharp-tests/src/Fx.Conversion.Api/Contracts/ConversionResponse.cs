namespace Fx.Conversion.Api.Contracts;

public sealed record ConversionResponse(MoneyDto Source, MoneyDto Result, decimal Rate, DateOnly RatesOf);
