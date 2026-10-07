using ClinicScheduling.Domain.Availability;

namespace ClinicScheduling.Domain.Appointments;

/// <summary>One booked visit of a patient with a practitioner, in a clinic or by video.</summary>
public sealed class Appointment
{
    private Appointment(
        Guid id,
        Guid patientId,
        Guid practitionerId,
        Guid clinicId,
        TimeRange time,
        bool isTelehealth,
        Guid? seriesId)
    {
        Id = id;
        PatientId = patientId;
        PractitionerId = practitionerId;
        ClinicId = clinicId;
        Time = time;
        IsTelehealth = isTelehealth;
        SeriesId = seriesId;
        Status = AppointmentStatus.Booked;
    }

    public Guid Id { get; }

    public Guid PatientId { get; }

    public Guid PractitionerId { get; }

    public Guid ClinicId { get; }

    public TimeRange Time { get; private set; }

    public bool IsTelehealth { get; }

    /// <summary>The treatment series this appointment belongs to, if it was booked as part of one.</summary>
    public Guid? SeriesId { get; }

    public AppointmentStatus Status { get; private set; }

    public CancellationReason? CancellationReason { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public int RescheduleCount { get; private set; }

    public static Appointment Book(
        Guid patientId,
        Guid practitionerId,
        Guid clinicId,
        TimeRange time,
        bool isTelehealth,
        Guid? seriesId = null) =>
        new(Guid.NewGuid(), patientId, practitionerId, clinicId, time, isTelehealth, seriesId);

    public void Cancel(CancellationReason reason, DateTimeOffset at)
    {
        EnsureBooked();
        Status = AppointmentStatus.Cancelled;
        CancellationReason = reason;
        CancelledAt = at;
    }

    public void Reschedule(TimeRange time)
    {
        EnsureBooked();
        Time = time;
        RescheduleCount++;
    }

    public void Complete()
    {
        EnsureBooked();
        Status = AppointmentStatus.Completed;
    }

    public void MarkNoShow()
    {
        EnsureBooked();
        Status = AppointmentStatus.NoShow;
    }

    private void EnsureBooked()
    {
        if (Status != AppointmentStatus.Booked)
        {
            throw new InvalidOperationException($"Appointment {Id} is {Status}, not booked.");
        }
    }
}
