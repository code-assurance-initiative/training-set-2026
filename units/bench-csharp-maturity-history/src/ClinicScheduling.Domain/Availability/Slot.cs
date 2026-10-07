namespace ClinicScheduling.Domain.Availability;

public sealed record Slot(Guid PractitionerId, DateTimeOffset Start, DateTimeOffset End, bool IsTelehealth);
