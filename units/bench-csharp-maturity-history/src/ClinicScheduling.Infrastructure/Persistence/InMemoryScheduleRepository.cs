using System.Collections.Concurrent;
using ClinicScheduling.Application.Abstractions;
using ClinicScheduling.Domain.Availability;

namespace ClinicScheduling.Infrastructure.Persistence;

public sealed class InMemoryScheduleRepository : IScheduleRepository
{
    private readonly ConcurrentDictionary<Guid, PractitionerSchedule> _schedules = new();

    public Task<PractitionerSchedule?> FindAsync(Guid practitionerId, CancellationToken cancellationToken) =>
        Task.FromResult(_schedules.GetValueOrDefault(practitionerId));

    public Task UpsertAsync(PractitionerSchedule schedule, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        _schedules[schedule.PractitionerId] = schedule;
        return Task.CompletedTask;
    }
}
