using Fx.Conversion.Monetary;
using Fx.Conversion.TestSupport;
using Shouldly;

namespace Fx.Conversion.UnitTests.Monetary;

public sealed class MoneyTests
{
    [Fact]
    public void AddsAmountsOfTheSameCurrency()
    {
        var sum = new Money(10.25m, KnownCurrencies.Eur).Plus(new Money(4.75m, KnownCurrencies.Eur));

        sum.ShouldBe(new Money(15.00m, KnownCurrencies.Eur));
    }

    [Fact]
    public void SubtractsAmountsOfTheSameCurrency()
    {
        var difference = new Money(10m, KnownCurrencies.Usd).Minus(new Money(12.5m, KnownCurrencies.Usd));

        difference.Amount.ShouldBe(-2.5m);
        difference.Currency.ShouldBe(KnownCurrencies.Usd);
    }

    [Fact]
    public void AddingDifferentCurrenciesThrows() =>
        Assert.Throws<InvalidOperationException>(() => new Money(1m, KnownCurrencies.Eur).Plus(new Money(1m, KnownCurrencies.Usd)));

    [Fact]
    public void ScalesAndNegates()
    {
        var money = new Money(2.50m, KnownCurrencies.Gbp);

        MoneyAssert.Equal(7.50m, "GBP", money.Times(3));
        MoneyAssert.Equal(-2.50m, "GBP", money.Negate());
    }

    [Fact]
    public void ZeroIsZeroInItsCurrency()
    {
        var zero = Money.Zero(KnownCurrencies.Jpy);

        zero.IsZero.ShouldBeTrue();
        zero.ToString().ShouldBe("0 JPY");
    }
}
