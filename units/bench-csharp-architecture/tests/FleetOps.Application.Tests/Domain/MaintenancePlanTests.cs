using FleetOps.Domain.Inspections;
using FleetOps.Domain.Maintenance;
using FleetOps.Domain.Vehicles;

namespace FleetOps.Application.Tests.Domain;

public sealed class MaintenancePlanTests
{
    [Fact]
    public void AServiceIsDueWhenTheOdometerCrossesAMultipleOfItsInterval()
    {
        var due = MaintenancePlan.Standard.DueServices(odometerKm: 30_500, lastServiceKm: 29_000);
        Assert.Equal(["Oil and filters", "Brake inspection"], due.Select(s => s.Name));
    }

    [Fact]
    public void NothingIsDueInsideOneInterval()
    {
        Assert.Empty(MaintenancePlan.Standard.DueServices(odometerKm: 16_000, lastServiceKm: 15_500));
    }

    [Fact]
    public void AnInspectionFailsOnAMajorOrDangerousDefectOnly()
    {
        Assert.True(Inspection.Record(VehicleId.New(), DateTimeOffset.UnixEpoch, [new Defect("Wiper worn", DefectSeverity.Minor)]).Passed);
        Assert.False(Inspection.Record(VehicleId.New(), DateTimeOffset.UnixEpoch, [new Defect("Brake line leak", DefectSeverity.Dangerous)]).Passed);
    }
}
