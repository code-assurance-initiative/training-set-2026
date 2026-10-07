namespace ClinicScheduling.IntegrationTests;

public sealed record AppointmentDto(Guid Id, Guid PatientId, string StartsAt, string Status, string? CancellationReason, int RescheduleCount, Guid? SeriesId);

public sealed record SlotDto(Guid PractitionerId, DateTimeOffset Start, DateTimeOffset End, bool IsTelehealth);

public sealed record CancellationDto(decimal Fee, bool CountsAsStrike, string Code);

public sealed record NoShowRowDto(Guid PractitionerId, int Appointments, int NoShows, int LateCancellations);
