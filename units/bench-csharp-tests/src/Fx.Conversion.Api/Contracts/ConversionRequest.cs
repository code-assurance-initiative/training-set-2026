using System.ComponentModel.DataAnnotations;
using Fx.Conversion.Rounding;

namespace Fx.Conversion.Api.Contracts;

public sealed record ConversionRequest : CurrencyPairRequest
{
    [Range(typeof(decimal), "-1000000000", "1000000000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Amount { get; init; }

    [EnumDataType(typeof(RoundingMode))]
    public RoundingMode Rounding { get; init; } = RoundingMode.HalfEven;
}
