using Fx.Conversion.Monetary;

namespace Fx.Conversion.Rounding;

/// <summary>Rounds amounts to the minor unit of their currency.</summary>
public static class MoneyRounding
{
    public static Money Round(Money money, RoundingMode mode) =>
        money with { Amount = Round(money.Amount, money.Currency.MinorUnits, mode) };

    public static decimal Round(decimal amount, int decimals, RoundingMode mode) => mode switch
    {
        RoundingMode.HalfEven => decimal.Round(amount, decimals, MidpointRounding.ToEven),
        RoundingMode.HalfUp => decimal.Round(amount, decimals, MidpointRounding.AwayFromZero),
        RoundingMode.Down => decimal.Round(amount, decimals, MidpointRounding.ToZero),
        RoundingMode.Up => AwayFromZero(amount, decimals),
        RoundingMode.Floor => decimal.Round(amount, decimals, MidpointRounding.ToNegativeInfinity),
        RoundingMode.Ceiling => decimal.Round(amount, decimals, MidpointRounding.ToPositiveInfinity),
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown rounding mode."),
    };

    private static decimal AwayFromZero(decimal amount, int decimals) =>
        amount >= 0m
            ? decimal.Round(amount, decimals, MidpointRounding.ToPositiveInfinity)
            : decimal.Round(amount, decimals, MidpointRounding.ToNegativeInfinity);
}
