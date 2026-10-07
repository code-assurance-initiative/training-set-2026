using System.ComponentModel.DataAnnotations;

namespace Fx.Conversion.Api.Contracts;

public sealed record AllocationRequest
{
    [Range(typeof(decimal), "-1000000000", "1000000000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Amount { get; init; }

    [Required]
    [RegularExpression(CurrencyCode.Pattern)]
    public string Currency { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public IReadOnlyList<int> Weights { get; init; } = [];
}
