using ClinicScheduling.Domain.Recurrence;

namespace ClinicScheduling.UnitTests.Recurrence;

public sealed class RecurrenceRuleTests
{
    [Fact]
    public void ParsesTheCompactForm()
    {
        Assert.True(RecurrenceRule.TryParse("weekly;interval=2;count=6;byday=th,mo", out var rule));

        Assert.NotNull(rule);
        Assert.Equal(2, rule.IntervalWeeks);
        Assert.Equal(6, rule.Count);
        Assert.Equal([DayOfWeek.Monday, DayOfWeek.Thursday], rule.Days);
        Assert.Equal("WEEKLY;INTERVAL=2;COUNT=6;BYDAY=MO,TH", rule.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("DAILY;COUNT=3;BYDAY=MO")]
    [InlineData("WEEKLY;COUNT=0;BYDAY=MO")]
    [InlineData("WEEKLY;COUNT=53;BYDAY=MO")]
    [InlineData("WEEKLY;COUNT=4")]
    [InlineData("WEEKLY;COUNT=4;BYDAY=XX")]
    [InlineData("WEEKLY;COUNT=4;BYDAY=MO;UNTIL=20261231")]
    public void RejectsWhatItCannotHonour(string? text)
    {
        Assert.False(RecurrenceRule.TryParse(text, out var rule));
        Assert.Null(rule);
    }
}
