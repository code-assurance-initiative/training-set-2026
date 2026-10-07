using System.ComponentModel.DataAnnotations;
using Invoicing.Contracts;

namespace Invoicing.Api.Requests;

public sealed class InvoiceLineRequest
{
    [Required, StringLength(300, MinimumLength = 1)]
    public string Description { get; init; } = string.Empty;

    [Range(typeof(decimal), "0.001", "1000000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Quantity { get; init; }

    [Range(typeof(decimal), "0", "100000000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal UnitPrice { get; init; }

    [Range(typeof(decimal), "0", "0.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal VatRate { get; init; }

    public InvoiceLine ToLine() => new(Description, Quantity, UnitPrice, VatRate);
}
