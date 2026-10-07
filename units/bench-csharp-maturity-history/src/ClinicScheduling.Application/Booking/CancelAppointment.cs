using ClinicScheduling.Application.Abstractions;
using ClinicScheduling.Domain.Appointments;
using ClinicScheduling.Domain.Policies;
using Microsoft.Extensions.Logging;

namespace ClinicScheduling.Application.Booking;

public sealed record CancelAppointmentCommand(Guid AppointmentId, CancellationReason Reason);

public sealed partial class CancelAppointmentHandler(
    IAppointmentRepository appointments,
    IPatientStrikeLedger strikes,
    IReminderOutbox reminders,
    CancellationPolicy policy,
    TimeProvider time,
    ILogger<CancelAppointmentHandler> logger)
{
    public async Task<OperationResult<CancellationDecision>> HandleAsync(CancelAppointmentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var appointment = await appointments.FindAsync(command.AppointmentId, cancellationToken).ConfigureAwait(false);
        if (appointment is null)
        {
            return Operation.NotFound<CancellationDecision>($"Appointment {command.AppointmentId} does not exist.");
        }

        if (appointment.Status != AppointmentStatus.Booked)
        {
            return Operation.Conflict<CancellationDecision>($"Appointment {appointment.Id} is already {appointment.Status}.");
        }

        var now = time.GetUtcNow();
        var decision = policy.EvaluateCancellation(appointment, command.Reason, now);
        appointment.Cancel(command.Reason, now);
        await appointments.SaveAsync(appointment, cancellationToken).ConfigureAwait(false);
        await reminders.WithdrawAsync(appointment.Id, cancellationToken).ConfigureAwait(false);
        if (decision.CountsAsStrike)
        {
            await strikes.RecordAsync(appointment.PatientId, now, cancellationToken).ConfigureAwait(false);
        }

        LogCancelled(appointment.Id, decision.Code, decision.Fee);
        return Operation.Ok<CancellationDecision>(decision);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Cancelled appointment {AppointmentId} ({Code}, fee {Fee})")]
    private partial void LogCancelled(Guid appointmentId, string code, decimal fee);
}
