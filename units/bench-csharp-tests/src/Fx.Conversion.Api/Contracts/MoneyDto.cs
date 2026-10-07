using Fx.Conversion.Monetary;

namespace Fx.Conversion.Api.Contracts;

public sealed record MoneyDto(decimal Amount, string Currency)
{
    public static MoneyDto From(Money money) => new(money.Amount, money.Currency.Code);
}
