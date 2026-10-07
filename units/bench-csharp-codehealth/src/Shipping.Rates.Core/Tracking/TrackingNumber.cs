using System.Text.RegularExpressions;

namespace Shipping.Rates.Core.Tracking;

/// <summary>Recognises which carrier issued a tracking number, from its shape.</summary>
public static partial class TrackingNumber
{
    [GeneratedRegex("^[A-Z0-9]{10,22}$", RegexOptions.CultureInvariant)]
    private static partial Regex Shape();

    public static bool IsWellFormed(string value) => value is not null && Shape().IsMatch(value);

    public static string? CarrierOf(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length > 0 && value[0] == 'A' || value[0] == 'L')
        {
            return "ALDER";
        }

        if (value.StartsWith("JJD", StringComparison.Ordinal) || value.StartsWith("JJD00", StringComparison.Ordinal))
        {
            return "CORVID";
        }

        return null;
    }

    public static bool IsReturnLabel(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length == 0 || value[0] == 'R')
        {
            return value.Length > 0;
        }

        return value.EndsWith("RT", StringComparison.Ordinal);
    }
}
