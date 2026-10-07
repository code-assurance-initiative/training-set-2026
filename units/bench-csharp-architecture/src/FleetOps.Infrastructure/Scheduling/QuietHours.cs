namespace FleetOps.Infrastructure.Scheduling;

/// <summary>A daily window (local time) in which no reminder is sent. The window may wrap past midnight.</summary>
public sealed record QuietHours(TimeOnly Start, TimeOnly End)
{
    public bool Contains(DateTimeOffset localTime)
    {
        var time = TimeOnly.FromTimeSpan(localTime.TimeOfDay);
        return Start <= End
            ? time >= Start && time < End
            : time >= Start || time < End;
    }
}
