namespace Invoicing.Contracts;

/// <summary>A seller or buyer as printed on the invoice.</summary>
public sealed class Party
{
    public Party(string name, string vatId, IReadOnlyList<string> addressLines, string countryCode)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("A party needs a name.", nameof(name)) : name;
        VatId = vatId ?? throw new ArgumentNullException(nameof(vatId));
        AddressLines = addressLines ?? throw new ArgumentNullException(nameof(addressLines));
        CountryCode = countryCode?.Length == 2
            ? countryCode.ToUpperInvariant()
            : throw new ArgumentException("Country codes are ISO 3166-1 alpha-2.", nameof(countryCode));
    }

    public string Name { get; }

    /// <summary>The VAT registration number, or an empty string for a buyer that has none.</summary>
    public string VatId { get; }

    public IReadOnlyList<string> AddressLines { get; }

    public string CountryCode { get; }
}
