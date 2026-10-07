using Shipping.Rates.Core.Domain;

namespace Shipping.Rates.Core.Labels;

/// <summary>Lays out addresses as label lines (UPU S42 order: company, name, street, postcode + city, country).</summary>
public static class AddressFormatter
{
    public static IReadOnlyList<string> FormatRecipient(Address recipient)
    {
        ArgumentNullException.ThrowIfNull(recipient);
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(recipient.Company))
        {
            lines.Add(recipient.Company.Trim());
        }

        lines.Add(recipient.Name.Trim());
        lines.Add(recipient.Street.Trim());
        lines.Add($"{recipient.PostalCode} {recipient.City}".Trim());
        lines.Add(recipient.CountryCode.ToUpperInvariant());
        return lines;
    }

    public static string SingleLine(Address address)
    {
        ArgumentNullException.ThrowIfNull(address);
        var parts = new[] { address.Company, address.Name, address.Street, $"{address.PostalCode} {address.City}", address.CountryCode };
        return string.Join(", ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }
}
