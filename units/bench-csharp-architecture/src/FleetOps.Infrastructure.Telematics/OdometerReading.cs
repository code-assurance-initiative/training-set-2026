namespace FleetOps.Infrastructure.Telematics;

public sealed record OdometerReading(string Vin, int Kilometres, DateTimeOffset RecordedAt);
