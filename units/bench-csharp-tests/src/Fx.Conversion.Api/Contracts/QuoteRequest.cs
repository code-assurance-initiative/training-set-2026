using System.ComponentModel.DataAnnotations;

namespace Fx.Conversion.Api.Contracts;

public sealed record QuoteRequest : CurrencyPairRequest
{
    [Range(typeof(decimal), "0.01", "1000000000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Amount { get; init; }
}
