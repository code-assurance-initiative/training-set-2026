using FleetOps.Domain.Common;
using FleetOps.Domain.Vehicles;

namespace FleetOps.Application.Tests.Domain;

public sealed class VehicleTests
{
    private static Vehicle NewVehicle(int odometer = 10_000) =>
        Vehicle.Register(new Vin("WVWZZZ1KZAW000001"), "ab 12 345", "Transit", odometer);

    [Fact]
    public void RegistrationIsNormalisedAndLastServiceStartsAtTheOdometer()
    {
        var vehicle = NewVehicle();
        Assert.Equal("AB 12 345", vehicle.Registration);
        Assert.Equal(10_000, vehicle.LastServiceKm);
        Assert.Equal(VehicleStatus.Active, vehicle.Status);
    }

    [Fact]
    public void OdometerNeverGoesBack()
    {
        var vehicle = NewVehicle();
        vehicle.RecordOdometer(12_000);
        Assert.Throws<DomainException>(() => vehicle.RecordOdometer(11_999));
        Assert.Equal(12_000, vehicle.OdometerKm);
    }

    [Fact]
    public void CompletingAServiceResetsTheServiceOdometerAndReturnsTheVehicle()
    {
        var vehicle = NewVehicle();
        vehicle.SendToWorkshop();
        vehicle.RecordOdometer(25_000);
        vehicle.CompleteService();
        Assert.Equal(25_000, vehicle.LastServiceKm);
        Assert.Equal(VehicleStatus.Active, vehicle.Status);
    }

    [Fact]
    public void ARetiredVehicleCannotGoToTheWorkshop()
    {
        var vehicle = NewVehicle();
        vehicle.Retire();
        Assert.Throws<DomainException>(vehicle.SendToWorkshop);
    }
}
