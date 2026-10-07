using Fx.Conversion.Rates;

namespace Fx.Conversion.TestSupport;

/// <summary>Rate tables with round, recognisable numbers, so expected amounts can be worked out by hand.</summary>
public static class RateTables
{
    /// <summary>EUR-based: 1 EUR = 1.10 USD = 0.85 GBP = 160 JPY = 0.95 CHF = 7.46 DKK = 11.20 SEK.</summary>
    public static RateTable Typical(DateOnly publishedOn) => new("EUR", publishedOn, new Dictionary<string, decimal>
    {
        ["USD"] = 1.10m,
        ["GBP"] = 0.85m,
        ["JPY"] = 160m,
        ["CHF"] = 0.95m,
        ["DKK"] = 7.46m,
        ["SEK"] = 11.20m,
    });

    public static RateTable Typical() => Typical(new DateOnly(2026, 3, 2));
}
