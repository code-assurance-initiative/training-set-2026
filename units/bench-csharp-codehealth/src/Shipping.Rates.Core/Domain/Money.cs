using System.Globalization;

namespace Shipping.Rates.Core.Domain;

/// <summary>An amount in one currency (ISO 4217). Arithmetic across currencies is refused.</summary>
public readonly record struct Money(decimal Amount, string Currency)
{
    public static Money Zero(string currency) => new(0m, currency);

    public static Money operator +(Money left, Money right)
    {
        if (!string.Equals(left.Currency, right.Currency, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Cannot add {right.Currency} to {left.Currency}.");
        }

        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public Money Add(Money other) => this + other;

    public Money Rounded() => this with { Amount = Math.Round(Amount, 2, MidpointRounding.AwayFromZero) };

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Amount:0.00} {Currency}");
}
