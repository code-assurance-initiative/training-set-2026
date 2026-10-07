using System.ComponentModel.DataAnnotations;
using Invoicing.Contracts;

namespace Invoicing.Api.Requests;

public sealed class PartyRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [StringLength(20)]
    [RegularExpression("^[A-Z0-9]*$")]
    public string VatId { get; init; } = string.Empty;

    [Required, MinLength(1), MaxLength(5)]
    public IReadOnlyList<string> AddressLines { get; init; } = [];

    [Required, RegularExpression("^[A-Z]{2}$")]
    public string CountryCode { get; init; } = string.Empty;

    public Party ToParty() => new(Name, VatId, AddressLines, CountryCode);
}
