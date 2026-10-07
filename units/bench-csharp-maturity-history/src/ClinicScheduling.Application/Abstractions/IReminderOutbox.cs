using ClinicScheduling.Application.Reminders;

namespace ClinicScheduling.Application.Abstractions;

/// <summary>Planned reminders waiting to be sent by the dispatch worker (ADR 0005).</summary>
public interface IReminderOutbox
{
    Task ScheduleAsync(IEnumerable<PlannedReminder> reminders, CancellationToken cancellationToken);

    /// <summary>Withdraws every unsent reminder of the appointment.</summary>
    Task WithdrawAsync(Guid appointmentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlannedReminder>> DueAsync(DateTimeOffset now, int max, CancellationToken cancellationToken);

    Task MarkSentAsync(Guid reminderId, CancellationToken cancellationToken);
}
