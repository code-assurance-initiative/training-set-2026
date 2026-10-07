namespace ClinicScheduling.Domain.Availability;

/// <summary>
/// One block of a practitioner's weekly opening hours, in the clinic's local time. A practitioner may have several
/// blocks on one day (a split shift); a block may carry one break.
/// </summary>
public sealed record WeeklyHours(
    DayOfWeek Day,
    TimeOnly Opens,
    TimeOnly Closes,
    TimeOnly? BreakStarts = null,
    TimeOnly? BreakEnds = null,
    bool InClinicOnly = false);
