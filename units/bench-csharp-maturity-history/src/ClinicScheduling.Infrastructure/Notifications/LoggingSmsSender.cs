using Microsoft.Extensions.Logging;

namespace ClinicScheduling.Infrastructure.Notifications;

/// <summary>The development stand-in for the SMS gateway: logs the message length instead of sending it.</summary>
public sealed partial class LoggingSmsSender(ILogger<LoggingSmsSender> logger) : ISmsSender
{
    public Task SendAsync(Guid patientId, string text, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);
        LogSent(patientId, text.Length);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "SMS to patient {PatientId} ({Length} characters)")]
    private partial void LogSent(Guid patientId, int length);
}
