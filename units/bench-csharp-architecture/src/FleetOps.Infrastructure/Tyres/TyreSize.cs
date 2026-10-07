using System.Globalization;
using System.Text.RegularExpressions;

namespace FleetOps.Infrastructure.Tyres;

/// <summary>A tyre size in ISO metric notation, e.g. <c>205/55R16</c>.</summary>
public sealed partial record TyreSize(int WidthMm, int AspectRatio, int RimInches)
{
    public static TyreSize Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var match = Notation().Match(value.Trim());
        if (!match.Success)
        {
            throw new FormatException($"'{value}' is not a tyre size such as 205/55R16.");
        }

        return new TyreSize(
            int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture));
    }

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{WidthMm}/{AspectRatio}R{RimInches}");

    [GeneratedRegex(@"^(\d{3})/(\d{2})\s?R(\d{2})$", RegexOptions.IgnoreCase)]
    private static partial Regex Notation();
}
