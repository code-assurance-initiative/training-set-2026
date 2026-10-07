namespace ClinicScheduling.Infrastructure.Notifications;

/// <summary>Delivers a text message to a patient. The production gateway resolves the patient's number itself.</summary>
public interface ISmsSender
{
    Task SendAsync(Guid patientId, string text, CancellationToken cancellationToken);
}
