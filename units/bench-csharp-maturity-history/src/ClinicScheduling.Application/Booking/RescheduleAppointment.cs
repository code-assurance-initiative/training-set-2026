using ClinicScheduling.Application.Abstractions;
using ClinicScheduling.Application.Reminders;
using ClinicScheduling.Domain.Appointments;
using ClinicScheduling.Domain.Availability;

namespace ClinicScheduling.Application.Booking;

public sealed record RescheduleAppointmentCommand(Guid AppointmentId, DateTimeOffset NewStart);

public sealed class RescheduleAppointmentHandler(
    IAppointmentRepository appointments,
    IScheduleRepository schedules,
    IReminderOutbox reminders,
    SlotFinder slotFinder,
    SlotSearchOptions searchOptions,
    ReminderPlanner planner)
{
    /// <summary>A patient may move one appointment this many times; after that it must be cancelled and rebooked.</summary>
    public const int MaxReschedules = 3;

    public async Task<OperationResult<Appointment>> HandleAsync(RescheduleAppointmentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var appointment = await appointments.FindAsync(command.AppointmentId, cancellationToken).ConfigureAwait(false);
        if (appointment is null)
        {
            return Operation.NotFound<Appointment>($"Appointment {command.AppointmentId} does not exist.");
        }

        if (appointment.Status != AppointmentStatus.Booked || appointment.RescheduleCount >= MaxReschedules)
        {
            return Operation.Conflict<Appointment>("This appointment can no longer be rescheduled.");
        }

        var schedule = await schedules.FindAsync(appointment.PractitionerId, cancellationToken).ConfigureAwait(false);
        if (schedule is null)
        {
            return Operation.NotFound<Appointment>($"Practitioner {appointment.PractitionerId} has no schedule.");
        }

        var day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(command.NewStart, schedule.TimeZone).DateTime);
        var from = command.NewStart.AddDays(-1);
        var others = await appointments.BookedForPractitionerAsync(schedule.PractitionerId, from, from.AddDays(3), cancellationToken).ConfigureAwait(false);
        var booked = others.Where(a => a.Id != appointment.Id).Select(a => a.Time).ToList();
        var query = new SlotQuery(day, day, appointment.Time.Duration, Telehealth: appointment.IsTelehealth);
        if (!slotFinder.FindOpenSlots(schedule, booked, query, searchOptions).Any(s => s.Start == command.NewStart))
        {
            return Operation.Conflict<Appointment>("The requested time is not an open slot.");
        }

        appointment.Reschedule(new TimeRange(command.NewStart, command.NewStart + appointment.Time.Duration));
        await appointments.SaveAsync(appointment, cancellationToken).ConfigureAwait(false);
        await reminders.WithdrawAsync(appointment.Id, cancellationToken).ConfigureAwait(false);
        await reminders.ScheduleAsync(planner.Plan(appointment, schedule.TimeZone), cancellationToken).ConfigureAwait(false);
        return Operation.Ok<Appointment>(appointment);
    }
}
