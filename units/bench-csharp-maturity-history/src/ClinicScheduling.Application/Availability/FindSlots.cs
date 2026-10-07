using ClinicScheduling.Application.Abstractions;
using ClinicScheduling.Domain.Availability;

namespace ClinicScheduling.Application.Availability;

public sealed class FindSlotsHandler(
    IScheduleRepository schedules,
    IAppointmentRepository appointments,
    SlotFinder slotFinder,
    SlotSearchOptions searchOptions)
{
    public async Task<OperationResult<IReadOnlyList<Slot>>> HandleAsync(Guid practitionerId, SlotQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var schedule = await schedules.FindAsync(practitionerId, cancellationToken).ConfigureAwait(false);
        if (schedule is null)
        {
            return Operation.NotFound<IReadOnlyList<Slot>>($"Practitioner {practitionerId} has no schedule.");
        }

        var from = new DateTimeOffset(query.From.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(-1);
        var to = new DateTimeOffset(query.To.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(2);
        var booked = await appointments.BookedForPractitionerAsync(practitionerId, from, to, cancellationToken).ConfigureAwait(false);
        var slots = slotFinder.FindOpenSlots(schedule, [.. booked.Select(a => a.Time)], query, searchOptions);
        return Operation.Ok<IReadOnlyList<Slot>>(slots);
    }
}
