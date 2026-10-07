namespace FleetOps.Domain.Inspections;

public readonly record struct InspectionId(Guid Value)
{
    public static InspectionId New() => new(Guid.NewGuid());
}
