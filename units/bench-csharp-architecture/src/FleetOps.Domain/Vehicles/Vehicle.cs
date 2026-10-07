using FleetOps.Domain.Common;

namespace FleetOps.Domain.Vehicles;

public sealed class Vehicle
{
    private Vehicle(VehicleId id, Vin vin, string registration, string model)
    {
        Id = id;
        Vin = vin;
        Registration = registration;
        Model = model;
        Status = VehicleStatus.Active;
    }

    public VehicleId Id { get; private set; }

    public Vin Vin { get; private set; }

    public string Registration { get; private set; }

    public string Model { get; private set; }

    public int OdometerKm { get; private set; }

    public int LastServiceKm { get; private set; }

    public VehicleStatus Status { get; private set; }

    public static Vehicle Register(Vin vin, string registration, string model, int odometerKm)
    {
        ArgumentNullException.ThrowIfNull(vin);
        ArgumentException.ThrowIfNullOrWhiteSpace(registration);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentOutOfRangeException.ThrowIfNegative(odometerKm);
        return new Vehicle(VehicleId.New(), vin, registration.Trim().ToUpperInvariant(), model.Trim())
        {
            OdometerKm = odometerKm,
            LastServiceKm = odometerKm,
        };
    }

    /// <summary>Odometers only move forward; a lower reading is a faulty unit or a typo and is rejected.</summary>
    public void RecordOdometer(int kilometres)
    {
        if (kilometres < OdometerKm)
        {
            throw new DomainException($"Odometer cannot go back from {OdometerKm} km to {kilometres} km.");
        }

        OdometerKm = kilometres;
    }

    public void SendToWorkshop()
    {
        EnsureNotRetired();
        Status = VehicleStatus.InWorkshop;
    }

    public void CompleteService()
    {
        EnsureNotRetired();
        LastServiceKm = OdometerKm;
        Status = VehicleStatus.Active;
    }

    public void Retire() => Status = VehicleStatus.Retired;

    private void EnsureNotRetired()
    {
        if (Status == VehicleStatus.Retired)
        {
            throw new DomainException($"Vehicle {Registration} is retired.");
        }
    }
}
