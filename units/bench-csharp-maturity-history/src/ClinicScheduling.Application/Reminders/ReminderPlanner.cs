using ClinicScheduling.Domain.Appointments;
using Microsoft.Extensions.Options;

namespace ClinicScheduling.Application.Reminders;

/// <summary>Decides when an appointment's reminders go out.</summary>
public sealed class ReminderPlanner(IOptions<ReminderOptions> options, TimeProvider time)
{
    public IReadOnlyList<PlannedReminder> Plan(Appointment appointment, TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(appointment);
        ArgumentNullException.ThrowIfNull(zone);

        var settings = options.Value;
        var now = time.GetUtcNow();
        var plan = new List<PlannedReminder>(2);
        foreach (var (kind, lead) in new[] { (ReminderKind.DayBefore, settings.DayBeforeLead), (ReminderKind.SameDay, settings.SameDayLead) })
        {
            var sendAt = OutsideQuietHours(appointment.Time.Start - lead, zone, settings);
            if (sendAt > now && sendAt < appointment.Time.Start)
            {
                plan.Add(new PlannedReminder(Guid.NewGuid(), appointment.Id, appointment.PatientId, sendAt, kind));
            }
        }

        return plan;
    }

    private static DateTimeOffset OutsideQuietHours(DateTimeOffset instant, TimeZoneInfo zone, ReminderOptions settings)
    {
        var local = TimeZoneInfo.ConvertTime(instant, zone);
        var clock = TimeOnly.FromDateTime(local.DateTime);
        if (!clock.IsBetween(settings.QuietHoursStart, settings.QuietHoursEnd))
        {
            return instant;
        }

        var eveningDay = clock >= settings.QuietHoursStart ? local.Date : local.Date.AddDays(-1);
        var evening = eveningDay + settings.QuietHoursStart.ToTimeSpan() - TimeSpan.FromMinutes(30);
        return new DateTimeOffset(evening, zone.GetUtcOffset(evening));
    }
}
