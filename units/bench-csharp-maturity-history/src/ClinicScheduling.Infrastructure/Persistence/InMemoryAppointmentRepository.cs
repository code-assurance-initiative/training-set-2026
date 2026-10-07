using System.Collections.Concurrent;
using ClinicScheduling.Application.Abstractions;
using ClinicScheduling.Domain.Appointments;

namespace ClinicScheduling.Infrastructure.Persistence;

/// <summary>Process-memory store: appointments live as long as the process (see README, "Persistence").</summary>
public sealed class InMemoryAppointmentRepository : IAppointmentRepository
{
    private readonly ConcurrentDictionary<Guid, Appointment> _appointments = new();

    public Task<Appointment?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_appointments.GetValueOrDefault(id));

    public Task<IReadOnlyList<Appointment>> BookedForPractitionerAsync(
        Guid practitionerId, DateTimeOffset startsFrom, DateTimeOffset startsBefore, CancellationToken cancellationToken) =>
        Query(a => a.PractitionerId == practitionerId && a.Status == AppointmentStatus.Booked && a.Time.Start >= startsFrom && a.Time.Start < startsBefore);

    public Task<IReadOnlyList<Appointment>> ForPatientAsync(Guid patientId, CancellationToken cancellationToken) =>
        Query(a => a.PatientId == patientId);

    public Task<IReadOnlyList<Appointment>> StartingBetweenAsync(DateTimeOffset startsFrom, DateTimeOffset startsBefore, CancellationToken cancellationToken) =>
        Query(a => a.Time.Start >= startsFrom && a.Time.Start < startsBefore);

    public Task AddAsync(Appointment appointment, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(appointment);
        if (!_appointments.TryAdd(appointment.Id, appointment))
        {
            throw new InvalidOperationException($"Appointment {appointment.Id} already exists.");
        }

        return Task.CompletedTask;
    }

    public Task SaveAsync(Appointment appointment, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(appointment);
        _appointments[appointment.Id] = appointment;
        return Task.CompletedTask;
    }

    private Task<IReadOnlyList<Appointment>> Query(Func<Appointment, bool> predicate) =>
        Task.FromResult<IReadOnlyList<Appointment>>([.. _appointments.Values.Where(predicate).OrderBy(a => a.Time.Start)]);
}
