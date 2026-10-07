namespace Shipping.Rates.Core.Labels;

/// <summary>Label creation settings (bound from configuration section <c>Labels</c>).</summary>
public sealed class LabelOptions
{
    public const string SectionName = "Labels";

    /// <summary>Directory the label files and the label index are written to.</summary>
    public string StorageRoot { get; set; } = "labels";

    public string LabelPrefix { get; set; } = "SR";

    public string Currency { get; set; } = "EUR";

    public string? SmtpHost { get; set; }

    public int SmtpPort { get; set; } = 587;

    /// <summary>Sender address of the label e-mails; required for e-mail to be sent.</summary>
    public string? FromAddress { get; set; }

    public string EmailSignature { get; set; } = "The shipping desk";

    /// <summary>Labels and their index entries are deleted this many days after creation.</summary>
    public int RetentionDays { get; set; } = 90;

    /// <summary>Discount in percent per customer account id.</summary>
    public Dictionary<string, decimal> AccountDiscounts { get; set; } = new(StringComparer.Ordinal);
}
