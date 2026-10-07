using FleetOps.Contracts.Maintenance;
using FleetOps.Infrastructure.Email;
using Microsoft.Extensions.Options;

namespace FleetOps.Infrastructure.Scheduling;

/// <summary>Mails a reminder for every due service; reminders held back for quiet hours go out on the next run.</summary>
public sealed class ReminderDispatcher(ReminderMailer mailer, IOptions<SchedulingOptions> options, TimeProvider clock)
{
    public async Task<ReminderRun> DispatchAsync(IReadOnlyList<MaintenanceDue> due, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(due);
        var recipient = new EmailAddress(options.Value.ReminderRecipient);
        var sent = 0;
        foreach (var item in due)
        {
            if (await mailer.SendAsync(item, recipient, cancellationToken).ConfigureAwait(false))
            {
                sent++;
            }
        }

        return new ReminderRun(clock.GetUtcNow(), due.Count, sent, due.Count - sent);
    }
}
