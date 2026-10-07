namespace FleetOps.Infrastructure.Scheduling;

/// <summary>The next stretch of time outside quiet hours, starting now or when quiet hours end.</summary>
public sealed class ReminderWindowCalculator : IReminderWindowCalculator
{
    public ReminderWindow NextWindow(DateTimeOffset now, QuietHours quietHours)
    {
        ArgumentNullException.ThrowIfNull(quietHours);
        var start = now;
        if (quietHours.Contains(now))
        {
            var endToday = new DateTimeOffset(now.Date + quietHours.End.ToTimeSpan(), now.Offset);
            start = endToday > now ? endToday : endToday.AddDays(1);
        }

        var nextQuiet = new DateTimeOffset(start.Date + quietHours.Start.ToTimeSpan(), start.Offset);
        if (nextQuiet <= start)
        {
            nextQuiet = nextQuiet.AddDays(1);
        }

        return new ReminderWindow(start, nextQuiet);
    }
}
