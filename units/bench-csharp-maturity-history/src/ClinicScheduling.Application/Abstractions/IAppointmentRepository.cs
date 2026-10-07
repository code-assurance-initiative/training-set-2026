using ClinicScheduling.Domain.Appointments;

namespace ClinicScheduling.Application.Abstractions;

public interface IAppointmentRepository
{
    Task<Appointment?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The practitioner's appointments that are still booked and start within [startsFrom, startsBefore).</summary>
    Task<IReadOnlyList<Appointment>> BookedForPractitionerAsync(
        Guid practitionerId, DateTimeOffset startsFrom, DateTimeOffset startsBefore, CancellationToken cancellationToken);

    Task<IReadOnlyList<Appointment>> ForPatientAsync(Guid patientId, CancellationToken cancellationToken);

    /// <summary>Every appointment, whatever its status, that starts within [startsFrom, startsBefore).</summary>
    Task<IReadOnlyList<Appointment>> StartingBetweenAsync(DateTimeOffset startsFrom, DateTimeOffset startsBefore, CancellationToken cancellationToken);

    Task AddAsync(Appointment appointment, CancellationToken cancellationToken);

    Task SaveAsync(Appointment appointment, CancellationToken cancellationToken);
}
