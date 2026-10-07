using System.Globalization;

namespace Shipping.Rates.Tools.Import;

/// <summary>A rate card as the carriers deliver it: CSV with header <c>carrier,currency,zone,per_kg,base_fee</c>.</summary>
public sealed record RateCardFile(string Carrier, string Currency, decimal BaseFee, IReadOnlyDictionary<int, decimal> PerKgByZone)
{
    public string Describe() =>
        string.Create(CultureInfo.InvariantCulture, $"{Carrier}: base {BaseFee:0.00} {Currency}, {PerKgByZone.Count} zone(s)");
}

public static class RateCardCsv
{
    public static RateCardFile Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var rows = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (rows.Length < 2 || rows[0] != "carrier,currency,zone,per_kg,base_fee")
        {
            throw new FormatException("Expected the header carrier,currency,zone,per_kg,base_fee.");
        }

        string carrier = null;
        string currency = null;
        var baseFee = 0m;
        var perKg = new Dictionary<int, decimal>();
        foreach (var row in rows.Skip(1))
        {
            var cells = row.Split(',');
            if (cells.Length != 5)
            {
                throw new FormatException($"Expected 5 cells: {row}");
            }

            carrier ??= cells[0];
            currency ??= cells[1];
            if (cells[0] != carrier || cells[1] != currency)
            {
                throw new FormatException("A rate card holds one carrier in one currency.");
            }

            perKg[int.Parse(cells[2], CultureInfo.InvariantCulture)] = decimal.Parse(cells[3], CultureInfo.InvariantCulture);
            baseFee = decimal.Parse(cells[4], CultureInfo.InvariantCulture);
        }

        return new RateCardFile(carrier, currency, baseFee, perKg);
    }
}
