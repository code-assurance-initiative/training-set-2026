using Fx.Conversion.Currencies;
using Fx.Conversion.Monetary;

namespace Fx.Conversion.Rounding;

/// <summary>
/// Rounding for amounts settled in physical cash, where the smallest coin is larger than the currency's minor unit:
/// Swiss francs are paid in steps of 0.05, Danish kroner in 0.50, Swedish and Norwegian kroner in whole kroner. Card
/// and account payments keep the minor unit; only the cash leg of a till or kiosk transaction uses this.
/// </summary>
public static class CashRounding
{
    private static readonly Dictionary<string, decimal> Increments = new(StringComparer.Ordinal)
    {
        ["AUD"] = 0.05m,
        ["CAD"] = 0.05m,
        ["CHF"] = 0.05m,
        ["CZK"] = 1m,
        ["DKK"] = 0.50m,
        ["HUF"] = 5m,
        ["NOK"] = 1m,
        ["NZD"] = 0.10m,
        ["SEK"] = 1m,
    };

    /// <summary>True when cash payments in <paramref name="currency"/> settle in steps coarser than its minor unit.</summary>
    public static bool HasCashIncrement(Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        return Increments.ContainsKey(currency.Code);
    }

    /// <summary>The cash step for <paramref name="currency"/>; its minor unit when it has no coarser one.</summary>
    public static decimal IncrementFor(Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        return Increments.TryGetValue(currency.Code, out var step) ? step : currency.MinorUnit;
    }

    /// <summary>Rounds <paramref name="money"/> to a payable cash amount. Ties are resolved by <paramref name="mode"/>.</summary>
    public static Money RoundForCash(Money money, RoundingMode mode = RoundingMode.HalfUp)
    {
        var step = IncrementFor(money.Currency);
        if (step == money.Currency.MinorUnit)
        {
            return MoneyRounding.Round(money, mode);
        }

        var steps = MoneyRounding.Round(money.Amount / step, 0, mode);
        return money with { Amount = steps * step };
    }

    /// <summary>
    /// The amount that cash rounding added to (positive) or removed from (negative) a bill. Tills post it to a
    /// rounding account so the books balance with the card total.
    /// </summary>
    public static Money RoundingDifference(Money money, RoundingMode mode = RoundingMode.HalfUp)
    {
        var exact = MoneyRounding.Round(money, mode);
        return RoundForCash(money, mode).Minus(exact);
    }
}
