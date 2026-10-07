using System.Globalization;

namespace FleetOps.Infrastructure.Email;

public static class ReminderTemplate
{
    public static (string Subject, string Body) Render(string registration, string service, int currentKm) =>
        (string.Create(CultureInfo.InvariantCulture, $"{registration}: {service} is due"),
         string.Create(CultureInfo.InvariantCulture,
             $"Vehicle {registration} has reached {currentKm:N0} km and is due for: {service}.\r\nPlease book it into the workshop."));
}
