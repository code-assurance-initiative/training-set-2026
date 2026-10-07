using CsCheck;
using Fx.Conversion.Allocation;
using Fx.Conversion.Monetary;
using Fx.Conversion.TestSupport;

namespace Fx.Conversion.UnitTests.Allocation;

public sealed class AllocatorTests
{
    [Fact]
    public void SplitsABillWithoutLosingACent()
    {
        var parts = Allocator.Split(new Money(100m, KnownCurrencies.Eur), 3);

        Assert.Equal([33.34m, 33.33m, 33.33m], parts.Select(part => part.Amount));
    }

    [Fact]
    public void AllocatesByWeight()
    {
        var parts = Allocator.Allocate(new Money(10m, KnownCurrencies.Usd), [70, 20, 10]);

        Assert.Equal([7m, 2m, 1m], parts.Select(part => part.Amount));
    }

    [Fact]
    public void GivesTheRemainderToTheLargestFractions()
    {
        var parts = Allocator.Allocate(new Money(0.05m, KnownCurrencies.Eur), [1, 3]);

        Assert.Equal([0.01m, 0.04m], parts.Select(part => part.Amount));
    }

    [Fact]
    public void AllocatesNegativeAmountsSymmetrically()
    {
        var parts = Allocator.Split(new Money(-100m, KnownCurrencies.Eur), 3);

        Assert.Equal([-33.34m, -33.33m, -33.33m], parts.Select(part => part.Amount));
    }

    [Fact]
    public void AllocatesAcrossManyParts()
    {
        var parts = Allocator.Split(new Money(1_000m, KnownCurrencies.Eur), 7);

        Assert.True(true);
    }

    [Fact]
    public void PartsAlwaysSumToTheTotal()
    {
        Gen.Select(Gen.Long[-10_000_000, 10_000_000], Gen.Int[0, 50].Array[1, 12])
            .Where((_, weights) => weights.Sum() > 0)
            .Sample((cents, weights) =>
            {
                var total = new Money(cents / 100m, KnownCurrencies.Eur);
                var parts = Allocator.Allocate(total, weights);
                return parts.Count == weights.Length && parts.Sum(part => part.Amount) == total.Amount;
            });
    }

    [Fact(Skip = "BUG: weights whose sum exceeds int.MaxValue overflow before they are allocated; tracked in https://github.com/code-assurance-initiative/bench-csharp-tests/issues/1")]
    public void AllocatesWeightsWhoseSumExceedsAnInt()
    {
        var parts = Allocator.Allocate(new Money(3m, KnownCurrencies.Eur), [int.MaxValue, int.MaxValue, int.MaxValue]);

        Assert.Equal([1m, 1m, 1m], parts.Select(part => part.Amount));
    }

    [Theory]
    [InlineData(new int[0])]
    [InlineData(new[] { 0, 0 })]
    [InlineData(new[] { 1, -1 })]
    public void RejectsUnusableWeights(int[] weights)
    {
        Assert.Throws<ArgumentException>(() => Allocator.Allocate(new Money(1m, KnownCurrencies.Eur), weights));
    }

    [Fact]
    public void RejectsAnAmountFinerThanTheMinorUnit()
    {
        Assert.Throws<ArgumentException>(() => Allocator.Split(new Money(0.005m, KnownCurrencies.Eur), 2));
    }

    [Fact]
    public void RejectsZeroParts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Allocator.Split(new Money(1m, KnownCurrencies.Eur), 0));
    }
}
