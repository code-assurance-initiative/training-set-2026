namespace FleetOps.Contracts.Vehicles;

/// <summary>The last position the vehicle's telematics unit reported.</summary>
public sealed record VehiclePosition(double Latitude, double Longitude, DateTimeOffset RecordedAt);
