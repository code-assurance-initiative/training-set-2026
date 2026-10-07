namespace ClinicScheduling.Domain.Availability;

public sealed record SlotSearchOptions
{
    /// <summary>Slots start on multiples of this from the opening time.</summary>
    public TimeSpan Granularity { get; init; } = TimeSpan.FromMinutes(15);

    /// <summary>Kept free before and after every booked appointment.</summary>
    public TimeSpan BufferBetweenAppointments { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>Time to turn a treatment room around; an in-clinic visit must leave it before closing.</summary>
    public TimeSpan RoomTurnaround { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>The earliest a slot may start, measured from now.</summary>
    public TimeSpan MinimumNotice { get; init; } = TimeSpan.FromHours(2);

    /// <summary>After this local time no more same-day slots are offered.</summary>
    public TimeOnly SameDayCutoff { get; init; } = new(14, 0);

    public int MaxDaysAhead { get; init; } = 60;

    public int MaxResults { get; init; } = 200;
}
