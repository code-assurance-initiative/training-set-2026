using Fx.Conversion.Currencies;

namespace Fx.Conversion.Monetary;

/// <summary>An amount in one currency. Arithmetic is only defined between amounts of the same currency.</summary>
public readonly record struct Money(decimal Amount, Currency Currency)
{
    public static Money Zero(Currency currency) => new(0m, currency);

    public bool IsZero => Amount == 0m;

    public Money Plus(Money other)
    {
        EnsureSameCurrency(other);
        return this with { Amount = Amount + other.Amount };
    }

    public Money Minus(Money other)
    {
        EnsureSameCurrency(other);
        return this with { Amount = Amount - other.Amount };
    }

    public Money Times(decimal factor) => this with { Amount = Amount * factor };

    public Money Negate() => this with { Amount = -Amount };

    public override string ToString() => $"{Amount} {Currency.Code}";

    private void EnsureSameCurrency(Money other)
    {
        if (other.Currency != Currency)
        {
            throw new InvalidOperationException(
                $"Cannot combine {Currency.Code} with {other.Currency.Code}; convert one of them first.");
        }
    }
}
