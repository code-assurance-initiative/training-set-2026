using System.Globalization;

namespace Fx.Conversion.Monetary;

/// <summary>Formats amounts with their currency's minor units, followed by the ISO code ("1.234,50 EUR").</summary>
public static class MoneyFormatter
{
    public static string Format(Money money, IFormatProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        var digits = money.Currency.MinorUnits.ToString(CultureInfo.InvariantCulture);
        return string.Concat(money.Amount.ToString("N" + digits, provider), " ", money.Currency.Code);
    }

    /// <summary>Formats for display to the current user, in their culture.</summary>
    public static string FormatForDisplay(Money money) => Format(money, CultureInfo.CurrentCulture);
}
