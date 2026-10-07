using Microsoft.Extensions.Options;
using Shipping.Rates.Core.Pricing;
using Shipping.Rates.UnitTests.TestSupport;

namespace Shipping.Rates.UnitTests.Pricing;

public sealed class CutoffCalendarTests
{
    private static CutoffCalendar Calendar(string? cutoff) =>
        new(Options.Create(new ShippingOptions { DailyCutoff = cutoff }), Loggers.For<CutoffCalendar>());

    [Fact]
    public void Bookings_before_the_cutoff_ship_today()
    {
        Assert.Equal(new DateOnly(2026, 3, 10), Calendar("15:00").ShipDateFor(new DateTime(2026, 3, 10, 14, 59, 0)));
    }

    [Fact]
    public void Bookings_after_the_cutoff_on_friday_ship_on_monday()
    {
        Assert.Equal(new DateOnly(2026, 3, 16), Calendar("15:00").ShipDateFor(new DateTime(2026, 3, 13, 15, 0, 0)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("4pm")]
    public void Missing_or_unreadable_cutoffs_use_the_default(string? configured)
    {
        Assert.Equal(CutoffCalendar.DefaultCutoff, Calendar(configured).Cutoff);
    }
}
