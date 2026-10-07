namespace Invoicing.Contracts;

/// <summary>One priced line. Amounts are rounded per line, the way the ERP books them.</summary>
public sealed class InvoiceLine
{
    public InvoiceLine(string description, decimal quantity, decimal unitPrice, decimal vatRate)
    {
        Description = string.IsNullOrWhiteSpace(description)
            ? throw new ArgumentException("A line needs a description.", nameof(description))
            : description;
        Quantity = quantity > 0 ? quantity : throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        UnitPrice = unitPrice >= 0 ? unitPrice : throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");
        VatRate = vatRate is >= 0 and < 1 ? vatRate : throw new ArgumentOutOfRangeException(nameof(vatRate), "VAT rate is a fraction, e.g. 0.25.");
    }

    public string Description { get; }

    public decimal Quantity { get; }

    public decimal UnitPrice { get; }

    /// <summary>The VAT rate as a fraction (0.25 is 25 %).</summary>
    public decimal VatRate { get; }

    public decimal NetAmount => Math.Round(Quantity * UnitPrice, 2, MidpointRounding.AwayFromZero);

    public decimal VatAmount => Math.Round(NetAmount * VatRate, 2, MidpointRounding.AwayFromZero);
}
