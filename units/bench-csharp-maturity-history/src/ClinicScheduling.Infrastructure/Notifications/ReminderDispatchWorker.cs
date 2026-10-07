using ClinicScheduling.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClinicScheduling.Infrastructure.Notifications;

/// <summary>Sends due reminders from the outbox, outside any request (ADR 0005).</summary>
public sealed partial class ReminderDispatchWorker(
    IServiceScopeFactory scopes,
    IOptions<ReminderDispatchOptions> options,
    TimeProvider time,
    ILogger<ReminderDispatchWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.PollInterval, time);
        do
        {
            try
            {
                await DispatchDueAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogDispatchFailed(ex);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    internal async Task<int> DispatchDueAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var services = scope.ServiceProvider;
        var outbox = services.GetRequiredService<IReminderOutbox>();
        var appointments = services.GetRequiredService<IAppointmentRepository>();
        var schedules = services.GetRequiredService<IScheduleRepository>();
        var sender = services.GetRequiredService<ISmsSender>();

        var sent = 0;
        foreach (var reminder in await outbox.DueAsync(time.GetUtcNow(), options.Value.BatchSize, cancellationToken).ConfigureAwait(false))
        {
            var appointment = await appointments.FindAsync(reminder.AppointmentId, cancellationToken).ConfigureAwait(false);
            var schedule = appointment is null ? null : await schedules.FindAsync(appointment.PractitionerId, cancellationToken).ConfigureAwait(false);
            if (appointment is not null && schedule is not null)
            {
                var text = ReminderComposer.Compose(reminder, appointment, schedule, "your physiotherapist", options.Value.ClinicPhone);
                await sender.SendAsync(reminder.PatientId, text, cancellationToken).ConfigureAwait(false);
                sent++;
            }

            await outbox.MarkSentAsync(reminder.Id, cancellationToken).ConfigureAwait(false);
        }

        return sent;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Reminder dispatch failed; retrying on the next tick")]
    private partial void LogDispatchFailed(Exception exception);
}
