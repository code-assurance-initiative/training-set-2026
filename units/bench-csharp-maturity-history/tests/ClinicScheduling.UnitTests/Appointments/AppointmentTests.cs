using ClinicScheduling.Domain.Appointments;
using ClinicScheduling.Domain.Availability;
using static ClinicScheduling.UnitTests.TestData;

namespace ClinicScheduling.UnitTests.Appointments;

public sealed class AppointmentTests
{
    private static Appointment Booked() =>
        Appointment.Book(Guid.NewGuid(), PractitionerId, Guid.NewGuid(), new TimeRange(At(Monday, 9), At(Monday, 10)), false);

    [Fact]
    public void ABookedAppointmentCanBeCancelledOnce()
    {
        var appointment = Booked();

        appointment.Cancel(CancellationReason.PatientRequest, Now);

        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
        Assert.Equal(CancellationReason.PatientRequest, appointment.CancellationReason);
        Assert.Equal(Now, appointment.CancelledAt);
        Assert.Throws<InvalidOperationException>(() => appointment.Cancel(CancellationReason.PatientRequest, Now));
    }

    [Fact]
    public void ReschedulingMovesTheTimeAndCountsTheMove()
    {
        var appointment = Booked();
        var later = new TimeRange(At(Monday, 11), At(Monday, 12));

        appointment.Reschedule(later);

        Assert.Equal(later, appointment.Time);
        Assert.Equal(1, appointment.RescheduleCount);
    }

    [Fact]
    public void OnlyABookedAppointmentCanBeCompletedOrMissed()
    {
        var completed = Booked();
        completed.Complete();
        var missed = Booked();
        missed.MarkNoShow();

        Assert.Equal(AppointmentStatus.Completed, completed.Status);
        Assert.Equal(AppointmentStatus.NoShow, missed.Status);
        Assert.Throws<InvalidOperationException>(missed.Complete);
    }

    [Fact]
    public void ATimeRangeMustEndAfterItStarts()
    {
        Assert.Throws<ArgumentException>(() => new TimeRange(At(Monday, 10), At(Monday, 10)));
    }
}
