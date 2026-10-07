using ClinicScheduling.Application.Abstractions;
using ClinicScheduling.Application.Reminders;
using ClinicScheduling.Domain.Appointments;
using ClinicScheduling.Domain.Recurrence;

namespace ClinicScheduling.Application.Series;

public sealed record BookTreatmentSeriesCommand(
    Guid PatientId, Guid PractitionerId, Guid ClinicId, DateOnly FirstDay, TimeOnly StartTime, TimeSpan Duration, RecurrenceRule Rule);

/// <summary>Books every session of a treatment series, or none of them.</summary>
public sealed class BookTreatmentSeriesHandler(
    IScheduleRepository schedules,
    IAppointmentRepository appointments,
    IReminderOutbox reminders,
    RecurrenceExpander expander,
    ReminderPlanner planner)
{
    public async Task<OperationResult<IReadOnlyList<Appointment>>> HandleAsync(BookTreatmentSeriesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var schedule = await schedules.FindAsync(command.PractitionerId, cancellationToken).ConfigureAwait(false);
        if (schedule is null)
        {
            return Operation.NotFound<IReadOnlyList<Appointment>>($"Practitioner {command.PractitionerId} has no schedule.");
        }

        var sessions = expander.Expand(command.Rule, command.FirstDay, command.StartTime, command.Duration, schedule.TimeZone, schedule.CountryCode);
        if (sessions.Count < command.Rule.Count)
        {
            return Operation.Invalid<IReadOnlyList<Appointment>>("The series cannot be fitted around public holidays.");
        }

        var booked = await appointments.BookedForPractitionerAsync(
            command.PractitionerId, sessions[0].Start.AddDays(-1), sessions[^1].End.AddDays(1), cancellationToken).ConfigureAwait(false);
        var clashes = sessions.Where(s => booked.Any(b => b.Time.Overlaps(s))).Select(s => s.Start.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture)).ToList();
        if (clashes.Count > 0)
        {
            return Operation.Conflict<IReadOnlyList<Appointment>>($"The practitioner is already booked at {string.Join(", ", clashes)}.");
        }

        var seriesId = Guid.NewGuid();
        var created = new List<Appointment>(sessions.Count);
        foreach (var session in sessions)
        {
            var appointment = Appointment.Book(command.PatientId, command.PractitionerId, command.ClinicId, session, isTelehealth: false, seriesId);
            await appointments.AddAsync(appointment, cancellationToken).ConfigureAwait(false);
            await reminders.ScheduleAsync(planner.Plan(appointment, schedule.TimeZone), cancellationToken).ConfigureAwait(false);
            created.Add(appointment);
        }

        return Operation.Ok<IReadOnlyList<Appointment>>(created);
    }
}
