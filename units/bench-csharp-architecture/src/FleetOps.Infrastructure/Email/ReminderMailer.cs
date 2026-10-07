using FleetOps.Contracts.Maintenance;
using FleetOps.Infrastructure.Scheduling;

namespace FleetOps.Infrastructure.Email;

/// <summary>Sends one maintenance reminder, unless it would land inside the depot's quiet hours.</summary>
public sealed class ReminderMailer(IEmailSender sender, QuietHours quietHours, TimeProvider clock)
{
    /// <returns><c>false</c> when the reminder was held back for quiet hours.</returns>
    public async Task<bool> SendAsync(MaintenanceDue due, EmailAddress recipient, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(due);
        if (quietHours.Contains(clock.GetLocalNow()))
        {
            return false;
        }

        var (subject, body) = ReminderTemplate.Render(due.Registration, due.Service, due.CurrentKm);
        await sender.SendAsync(new EmailMessage(recipient, subject, body, EmailPriority.Normal, []), cancellationToken).ConfigureAwait(false);
        return true;
    }
}
