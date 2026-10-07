using Rentals.SharedKernel;

namespace Rentals.Lending.Tests.Domain;

public sealed class MoneyTests
{
    [Fact]
    public void Money_is_compared_by_value_and_normalises_currency() =>
        Assert.Equal(new Money(10m, "eur"), new Money(10.004m, "EUR"));

    [Fact]
    public void Money_rejects_negative_amounts_and_bad_currencies()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(-1m, "EUR"));
        Assert.Throws<ArgumentException>(() => new Money(1m, "EURO"));
    }

    [Fact]
    public void Adding_different_currencies_fails() =>
        Assert.Throws<InvalidOperationException>(() => new Money(1m, "EUR").Add(new Money(1m, "DKK")));

    [Fact]
    public void Multiply_scales_the_amount() =>
        Assert.Equal(new Money(12m, "EUR"), new Money(4m, "EUR").Multiply(3));
}
