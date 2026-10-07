using ClinicScheduling.Application.Reminders;
using ClinicScheduling.Domain.Appointments;
using ClinicScheduling.Domain.Availability;
using ClinicScheduling.Infrastructure.Notifications;
using static ClinicScheduling.UnitTests.TestData;

namespace ClinicScheduling.UnitTests.Notifications;

public sealed class ReminderComposerTests
{
    private static readonly DateTimeOffset Start = At(Monday.AddDays(3), 9);

    private static Appointment Booked(bool telehealth = false) =>
        Appointment.Book(Guid.NewGuid(), PractitionerId, Guid.NewGuid(), new TimeRange(Start, Start.AddMinutes(45)), telehealth);

    private static PlannedReminder Reminder(Appointment appointment, ReminderKind kind) =>
        new(Guid.NewGuid(), appointment.Id, appointment.PatientId, Start.AddHours(-2), kind);

    [Fact]
    public void WritesTheDayBeforeReminderInTheClinicsLanguage()
    {
        var appointment = Booked();

        var text = ReminderComposer.Compose(Reminder(appointment, ReminderKind.DayBefore), appointment, Schedule(), "Ida", "+45 11 22 33 44");

        Assert.StartsWith("Påmindelse: du har en tid hos Ida den ", text, StringComparison.Ordinal);
        Assert.Contains("+45 11 22 33 44", text, StringComparison.Ordinal);
        Assert.Contains(ReminderTexts.ByLanguage["da"]["cancellation-fee"], text, StringComparison.Ordinal);
    }

    [Fact]
    public void UsesTheVideoTextForTelehealth()
    {
        var appointment = Booked(telehealth: true);

        var text = ReminderComposer.Compose(Reminder(appointment, ReminderKind.SameDay), appointment, Schedule(), "Ida", "+45 11 22 33 44");

        Assert.StartsWith("Din videokonsultation med Ida starter kl. 09.00.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void FallsBackToEnglish()
    {
        var appointment = Booked();
        var swedish = new PractitionerSchedule(PractitionerId, "SE", TimeZoneInfo.Utc, Weekdays(new TimeOnly(8, 0), new TimeOnly(16, 0)), [], false, 8);

        var text = ReminderComposer.Compose(Reminder(appointment, ReminderKind.SameDay), appointment, swedish, "Ida", "+46 8 00 00 00");

        Assert.StartsWith("See you soon: your appointment with Ida is today at ", text, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryLanguageHasEveryText()
    {
        var keys = ReminderTexts.ByLanguage[ReminderTexts.DefaultLanguage].Keys.Order().ToList();

        Assert.All(ReminderTexts.ByLanguage.Values, texts => Assert.Equal(keys, texts.Keys.Order()));
    }
}
