using ClinicScheduling.Domain.Holidays;

namespace ClinicScheduling.Domain.Availability;

/// <summary>
/// Finds the bookable slots of one practitioner: opening hours minus breaks, holidays, existing appointments (with
/// their buffers), notice periods and the practitioner's daily cap.
/// </summary>
public sealed class SlotFinder(IHolidayCalendar holidays, TimeProvider time)
{
    public IReadOnlyList<Slot> FindOpenSlots(
        PractitionerSchedule schedule,
        IReadOnlyCollection<TimeRange> booked,
        SlotQuery query,
        SlotSearchOptions options)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(booked);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(options);

        if (query.Duration <= TimeSpan.Zero || query.To < query.From)
        {
            return [];
        }

        if (query.Telehealth && !schedule.OffersTelehealth)
        {
            return [];
        }

        if (query.RequiredSkill is not null && !schedule.HasSkill(query.RequiredSkill))
        {
            return [];
        }

        var now = time.GetUtcNow();
        var localNow = TimeZoneInfo.ConvertTime(now, schedule.TimeZone);
        var today = DateOnly.FromDateTime(localNow.DateTime);
        var lastDay = today.AddDays(options.MaxDaysAhead);
        var earliestStart = now + options.MinimumNotice;
        var slots = new List<Slot>();

        for (var day = query.From; day <= query.To && day <= lastDay; day = day.AddDays(1))
        {
            if (day < today || holidays.IsHoliday(schedule.CountryCode, day))
            {
                continue;
            }

            if (day == today && TimeOnly.FromDateTime(localNow.DateTime) >= options.SameDayCutoff)
            {
                continue;
            }

            var bookedThatDay = booked.Count(b => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(b.Start, schedule.TimeZone).DateTime) == day);
            if (bookedThatDay >= schedule.MaxAppointmentsPerDay)
            {
                continue;
            }

            foreach (var hours in schedule.HoursOn(day.DayOfWeek))
            {
                if (query.Telehealth && hours.InClinicOnly)
                {
                    continue;
                }

                var opens = At(day, hours.Opens, schedule.TimeZone);
                var closes = At(day, hours.Closes, schedule.TimeZone);
                var lastEnd = query.Telehealth ? closes : closes - options.RoomTurnaround;
                var cursor = opens;

                while (cursor + query.Duration <= lastEnd)
                {
                    var candidate = new TimeRange(cursor, cursor + query.Duration);

                    if (hours.BreakStarts is { } breakStarts && hours.BreakEnds is { } breakEnds)
                    {
                        var lunch = new TimeRange(At(day, breakStarts, schedule.TimeZone), At(day, breakEnds, schedule.TimeZone));
                        if (candidate.Overlaps(lunch))
                        {
                            cursor = lunch.End;
                            continue;
                        }
                    }

                    if (candidate.Start < earliestStart)
                    {
                        cursor += options.Granularity;
                        continue;
                    }

                    var clash = booked
                        .Where(b => b.Widen(options.BufferBetweenAppointments).Overlaps(candidate))
                        .OrderByDescending(b => b.End)
                        .FirstOrDefault();
                    if (clash != default)
                    {
                        cursor = AlignUp(clash.End + options.BufferBetweenAppointments, opens, options.Granularity);
                        continue;
                    }

                    slots.Add(new Slot(schedule.PractitionerId, candidate.Start, candidate.End, query.Telehealth));
                    if (slots.Count >= options.MaxResults)
                    {
                        return slots;
                    }

                    cursor += options.Granularity;
                }
            }
        }

        return slots;
    }

    private static DateTimeOffset At(DateOnly day, TimeOnly time, TimeZoneInfo zone)
    {
        var local = day.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, zone.GetUtcOffset(local));
    }

    /// <summary>The first slot boundary (opening time plus a whole number of steps) at or after <paramref name="value"/>.</summary>
    private static DateTimeOffset AlignUp(DateTimeOffset value, DateTimeOffset origin, TimeSpan step)
    {
        var steps = Math.Ceiling((value - origin).Ticks / (double)step.Ticks);
        return origin + TimeSpan.FromTicks((long)steps * step.Ticks);
    }
}
