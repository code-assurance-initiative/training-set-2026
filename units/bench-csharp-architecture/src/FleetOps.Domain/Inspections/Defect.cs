namespace FleetOps.Domain.Inspections;

public sealed record Defect(string Description, DefectSeverity Severity)
{
    /// <summary>Major and dangerous defects take the vehicle off the road until repaired.</summary>
    public bool TakesVehicleOffRoad => Severity >= DefectSeverity.Major;
}
