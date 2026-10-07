using System.Globalization;
using System.Text;

namespace DocumentExport.Api.Exports;

/// <summary>Renders inventory rows as RFC 4180 CSV (UTF-8, CRLF line endings).</summary>
public static class InventoryCsvWriter
{
    private const string Header = "sku,description,quantity,bin_location";

    public static byte[] Write(IEnumerable<InventoryRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var csv = new StringBuilder(Header).Append("\r\n");
        foreach (var row in rows)
        {
            csv.Append(Field(row.Sku)).Append(',')
               .Append(Field(row.Description)).Append(',')
               .Append(row.Quantity.ToString(CultureInfo.InvariantCulture)).Append(',')
               .Append(Field(row.BinLocation)).Append("\r\n");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    /// <summary>
    /// Quotes a field when needed, and neutralises a leading formula character so spreadsheet applications show
    /// the text instead of evaluating it.
    /// </summary>
    public static string Field(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
        {
            value = "'" + value;
        }

        var needsQuotes = value.AsSpan().IndexOfAny(",\"\r\n") >= 0;
        return needsQuotes ? "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"" : value;
    }
}
