using System.Collections.Concurrent;
using ClinicScheduling.Application.Abstractions;
using ClinicScheduling.Application.Reminders;

namespace ClinicScheduling.Infrastructure.Persistence;

public sealed class InMemoryReminderOutbox : IReminderOutbox
{
    private readonly ConcurrentDictionary<Guid, PlannedReminder> _pending = new();

    public Task ScheduleAsync(IEnumerable<PlannedReminder> reminders, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reminders);
        foreach (var reminder in reminders)
        {
            _pending[reminder.Id] = reminder;
        }

        return Task.CompletedTask;
    }

    public Task WithdrawAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        foreach (var reminder in _pending.Values.Where(r => r.AppointmentId == appointmentId))
        {
            _pending.TryRemove(reminder.Id, out _);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PlannedReminder>> DueAsync(DateTimeOffset now, int max, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PlannedReminder>>([.. _pending.Values.Where(r => r.SendAt <= now).OrderBy(r => r.SendAt).Take(max)]);

    public Task MarkSentAsync(Guid reminderId, CancellationToken cancellationToken)
    {
        _pending.TryRemove(reminderId, out _);
        return Task.CompletedTask;
    }
}
