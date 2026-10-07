using System.Globalization;
using ClinicScheduling.Application.Reminders;
using ClinicScheduling.Domain.Appointments;
using ClinicScheduling.Domain.Availability;

namespace ClinicScheduling.Infrastructure.Notifications;

/// <summary>Turns a planned reminder into the text that is sent, in the clinic's language.</summary>
public static class ReminderComposer
{
    private static readonly Dictionary<string, string> LanguageByCountry = new(StringComparer.OrdinalIgnoreCase)
    {
        ["DK"] = "da",
        ["DE"] = "de",
    };

    public static string Compose(PlannedReminder reminder, Appointment appointment, PractitionerSchedule schedule, string practitionerName, string clinicPhone)
    {
        ArgumentNullException.ThrowIfNull(reminder);
        ArgumentNullException.ThrowIfNull(appointment);
        ArgumentNullException.ThrowIfNull(schedule);

        var language = LanguageByCountry.GetValueOrDefault(schedule.CountryCode, ReminderTexts.DefaultLanguage);
        var texts = ReminderTexts.ByLanguage[language];
        var key = (reminder.Kind, appointment.IsTelehealth) switch
        {
            (ReminderKind.DayBefore, false) => ReminderTexts.DayBefore,
            (ReminderKind.DayBefore, true) => ReminderTexts.DayBeforeVideo,
            (ReminderKind.SameDay, false) => ReminderTexts.SameDay,
            _ => ReminderTexts.SameDayVideo,
        };

        var culture = CultureInfo.GetCultureInfo(language);
        var local = TimeZoneInfo.ConvertTime(appointment.Time.Start, schedule.TimeZone);
        var when = reminder.Kind == ReminderKind.SameDay ? local.ToString("t", culture) : local.ToString("g", culture);
        var body = string.Format(culture, texts[key], practitionerName, when, clinicPhone);
        return reminder.Kind == ReminderKind.DayBefore
            ? $"{body} {texts["cancellation-fee"]} {texts["signature"]}"
            : $"{body} {texts["signature"]}";
    }
}
