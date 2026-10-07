using System.ComponentModel.DataAnnotations;

namespace Fx.Conversion.Api.Contracts;

/// <summary>The source and target currency of a conversion or quote request.</summary>
public abstract record CurrencyPairRequest
{
    [Required]
    [RegularExpression(CurrencyCode.Pattern)]
    public string From { get; init; } = string.Empty;

    [Required]
    [RegularExpression(CurrencyCode.Pattern)]
    public string To { get; init; } = string.Empty;
}
