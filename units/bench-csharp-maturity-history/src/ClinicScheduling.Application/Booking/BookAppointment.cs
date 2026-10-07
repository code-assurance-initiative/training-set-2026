using ClinicScheduling.Application.Abstractions;
using ClinicScheduling.Application.Reminders;
using ClinicScheduling.Domain.Appointments;
using ClinicScheduling.Domain.Availability;
using ClinicScheduling.Domain.Policies;
using Microsoft.Extensions.Logging;

namespace ClinicScheduling.Application.Booking;

public sealed record BookAppointmentCommand(
    Guid PatientId, Guid PractitionerId, Guid ClinicId, DateTimeOffset Start, TimeSpan Duration, bool Telehealth);

public sealed partial class BookAppointmentHandler(
    IScheduleRepository schedules,
    IAppointmentRepository appointments,
    IPatientStrikeLedger strikes,
    IReminderOutbox reminders,
    SlotFinder slotFinder,
    SlotSearchOptions searchOptions,
    CancellationPolicy policy,
    ReminderPlanner planner,
    TimeProvider time,
    ILogger<BookAppointmentHandler> logger)
{
    public async Task<OperationResult<Appointment>> HandleAsync(BookAppointmentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var schedule = await schedules.FindAsync(command.PractitionerId, cancellationToken).ConfigureAwait(false);
        if (schedule is null)
        {
            return Operation.NotFound<Appointment>($"Practitioner {command.PractitionerId} has no schedule.");
        }

        var history = await strikes.StrikesAsync(command.PatientId, cancellationToken).ConfigureAwait(false);
        if (policy.RequiresDeposit(history, time.GetUtcNow()))
        {
            return Operation.Invalid<Appointment>("A deposit is required before this patient can book again.");
        }

        var day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(command.Start, schedule.TimeZone).DateTime);
        var booked = await BookedRangesAsync(schedule, day, cancellationToken).ConfigureAwait(false);
        var open = slotFinder.FindOpenSlots(schedule, booked, new SlotQuery(day, day, command.Duration, Telehealth: command.Telehealth), searchOptions);
        if (!open.Any(s => s.Start == command.Start))
        {
            return Operation.Conflict<Appointment>("The requested time is not an open slot.");
        }

        var appointment = Appointment.Book(
            command.PatientId, command.PractitionerId, command.ClinicId,
            new TimeRange(command.Start, command.Start + command.Duration), command.Telehealth);
        await appointments.AddAsync(appointment, cancellationToken).ConfigureAwait(false);
        await reminders.ScheduleAsync(planner.Plan(appointment, schedule.TimeZone), cancellationToken).ConfigureAwait(false);
        LogBooked(appointment.Id, appointment.PractitionerId, appointment.Time.Start);
        return Operation.Ok<Appointment>(appointment);
    }

    private async Task<IReadOnlyCollection<TimeRange>> BookedRangesAsync(PractitionerSchedule schedule, DateOnly day, CancellationToken cancellationToken)
    {
        var from = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(-1);
        var existing = await appointments.BookedForPractitionerAsync(schedule.PractitionerId, from, from.AddDays(3), cancellationToken).ConfigureAwait(false);
        return [.. existing.Select(a => a.Time)];
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Booked appointment {AppointmentId} with practitioner {PractitionerId} at {Start}")]
    private partial void LogBooked(Guid appointmentId, Guid practitionerId, DateTimeOffset start);
}
