using ClinicScheduling.Application.Abstractions;
using ClinicScheduling.Domain.Appointments;
using ClinicScheduling.Domain.Policies;

namespace ClinicScheduling.Application.Booking;

public sealed class RecordNoShowHandler(
    IAppointmentRepository appointments,
    IPatientStrikeLedger strikes,
    CancellationPolicy policy,
    TimeProvider time)
{
    public async Task<OperationResult<CancellationDecision>> HandleAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        var appointment = await appointments.FindAsync(appointmentId, cancellationToken).ConfigureAwait(false);
        if (appointment is null)
        {
            return Operation.NotFound<CancellationDecision>($"Appointment {appointmentId} does not exist.");
        }

        var now = time.GetUtcNow();
        if (appointment.Status != AppointmentStatus.Booked || appointment.Time.Start > now)
        {
            return Operation.Conflict<CancellationDecision>("Only a booked appointment that has started can be a no-show.");
        }

        appointment.MarkNoShow();
        await appointments.SaveAsync(appointment, cancellationToken).ConfigureAwait(false);
        await strikes.RecordAsync(appointment.PatientId, now, cancellationToken).ConfigureAwait(false);
        return Operation.Ok<CancellationDecision>(policy.EvaluateNoShow(appointment));
    }
}
