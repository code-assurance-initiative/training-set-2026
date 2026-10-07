namespace Fx.Conversion.Currencies;

/// <summary>
/// The currencies published in the ECB reference-rate feed, plus the euro, with their ISO 4217 minor units.
/// </summary>
internal static class Iso4217Table
{
    internal static readonly IReadOnlyDictionary<string, int> MinorUnits = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["AUD"] = 2,
        ["BRL"] = 2,
        ["CAD"] = 2,
        ["CHF"] = 2,
        ["CNY"] = 2,
        ["CZK"] = 2,
        ["DKK"] = 2,
        ["EUR"] = 2,
        ["GBP"] = 2,
        ["HKD"] = 2,
        ["HUF"] = 2,
        ["IDR"] = 2,
        ["ILS"] = 2,
        ["INR"] = 2,
        ["ISK"] = 0,
        ["JPY"] = 0,
        ["KRW"] = 0,
        ["MXN"] = 2,
        ["MYR"] = 2,
        ["NOK"] = 2,
        ["NZD"] = 2,
        ["PHP"] = 2,
        ["PLN"] = 2,
        ["RON"] = 2,
        ["SEK"] = 2,
        ["SGD"] = 2,
        ["THB"] = 2,
        ["TRY"] = 2,
        ["USD"] = 2,
        ["ZAR"] = 2,
    };

    /// <summary>True for a code of exactly three ASCII upper-case letters.</summary>
    internal static bool IsWellFormed(string? code) =>
        code is { Length: 3 } && code.All(char.IsAsciiLetterUpper);
}
