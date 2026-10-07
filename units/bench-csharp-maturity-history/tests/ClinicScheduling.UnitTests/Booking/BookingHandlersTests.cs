using ClinicScheduling.Application;
using ClinicScheduling.Application.Booking;
using ClinicScheduling.Application.Reminders;
using ClinicScheduling.Domain.Appointments;
using ClinicScheduling.Domain.Availability;
using ClinicScheduling.Domain.Policies;
using ClinicScheduling.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using static ClinicScheduling.UnitTests.TestData;

namespace ClinicScheduling.UnitTests.Booking;

public sealed class BookingHandlersTests
{
    private readonly FakeTimeProvider _time = new(Now);
    private readonly InMemoryScheduleRepository _schedules = new();
    private readonly InMemoryAppointmentRepository _appointments = new();
    private readonly InMemoryStrikeLedger _strikes = new();
    private readonly InMemoryReminderOutbox _outbox = new();
    private readonly CancellationPolicy _policy = new(new CancellationPolicyOptions());

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private BookAppointmentHandler Booker() => new(
        _schedules, _appointments, _strikes, _outbox, new SlotFinder(new FixedHolidays(), _time), new SlotSearchOptions(), _policy,
        new ReminderPlanner(Options.Create(new ReminderOptions()), _time), _time, NullLogger<BookAppointmentHandler>.Instance);

    private CancelAppointmentHandler Canceller() =>
        new(_appointments, _strikes, _outbox, _policy, _time, NullLogger<CancelAppointmentHandler>.Instance);

    private static BookAppointmentCommand Command(Guid patientId, DateTimeOffset start) =>
        new(patientId, PractitionerId, Guid.NewGuid(), start, TimeSpan.FromMinutes(30), false);

    [Fact]
    public async Task BooksAnOpenSlotAndPlansItsReminders()
    {
        await _schedules.UpsertAsync(Schedule(), Token);

        var result = await Booker().HandleAsync(Command(Guid.NewGuid(), At(Monday.AddDays(3), 10)), Token);

        Assert.Equal(OperationStatus.Ok, result.Status);
        Assert.Equal(2, (await _outbox.DueAsync(At(Monday.AddDays(4), 0), 10, Token)).Count);
    }

    [Fact]
    public async Task RefusesATakenSlot()
    {
        await _schedules.UpsertAsync(Schedule(), Token);
        await Booker().HandleAsync(Command(Guid.NewGuid(), At(Monday.AddDays(3), 10)), Token);

        var second = await Booker().HandleAsync(Command(Guid.NewGuid(), At(Monday.AddDays(3), 10)), Token);

        Assert.Equal(OperationStatus.Conflict, second.Status);
    }

    [Fact]
    public async Task UnknownPractitionersAreNotFound()
    {
        var result = await Booker().HandleAsync(Command(Guid.NewGuid(), At(Monday.AddDays(3), 10)), Token);

        Assert.Equal(OperationStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task LateCancellationsAddUpToADeposit()
    {
        await _schedules.UpsertAsync(Schedule(), Token);
        var patient = Guid.NewGuid();
        foreach (var hour in new[] { 10, 11 })
        {
            var booked = await Booker().HandleAsync(Command(patient, At(Monday, hour)), Token);
            Assert.NotNull(booked.Value);
            var cancelled = await Canceller().HandleAsync(new CancelAppointmentCommand(booked.Value.Id, CancellationReason.PatientRequest), Token);
            Assert.Equal(CancellationPolicy.LateCancellation, cancelled.Value?.Code);
        }

        var third = await Booker().HandleAsync(Command(patient, At(Monday.AddDays(3), 10)), Token);

        Assert.Equal(OperationStatus.Invalid, third.Status);
    }

    [Fact]
    public async Task CancellingWithdrawsTheReminders()
    {
        await _schedules.UpsertAsync(Schedule(), Token);
        var booked = await Booker().HandleAsync(Command(Guid.NewGuid(), At(Monday.AddDays(3), 10)), Token);
        Assert.NotNull(booked.Value);

        await Canceller().HandleAsync(new CancelAppointmentCommand(booked.Value.Id, CancellationReason.PatientRequest), Token);

        Assert.Empty(await _outbox.DueAsync(At(Monday.AddDays(4), 0), 10, Token));
    }
}
