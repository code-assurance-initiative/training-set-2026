namespace Shipping.Rates.Core.Labels;

#pragma warning disable CS8618

/// <summary>One entry of the label index file (labels/index.json).</summary>
public sealed class LabelRecord
{
    public string LabelId { get; set; }

    public string Carrier { get; set; }

    public string TrackingNumber { get; set; }

    public string ServiceCode { get; set; }

    public string RecipientEmail { get; set; }

    public decimal Price { get; set; }

    public string Currency { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public bool Voided { get; set; }
}
