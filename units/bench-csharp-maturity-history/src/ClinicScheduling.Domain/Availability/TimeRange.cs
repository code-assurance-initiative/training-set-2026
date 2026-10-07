namespace ClinicScheduling.Domain.Availability;

/// <summary>A half-open interval of time, [<see cref="Start"/>, <see cref="End"/>).</summary>
public readonly record struct TimeRange
{
    public TimeRange(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start)
        {
            throw new ArgumentException("A time range must end after it starts.", nameof(end));
        }

        Start = start;
        End = end;
    }

    public DateTimeOffset Start { get; }

    public DateTimeOffset End { get; }

    public TimeSpan Duration => End - Start;

    public bool Overlaps(TimeRange other) => Start < other.End && other.Start < End;

    /// <summary>The same range grown by <paramref name="margin"/> on both sides.</summary>
    public TimeRange Widen(TimeSpan margin) => new(Start - margin, End + margin);
}
