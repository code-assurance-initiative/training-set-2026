namespace Rentals.Lending.Domain.Maintenance;

public readonly record struct MaintenanceRecordId(Guid Value)
{
    public static MaintenanceRecordId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("N");
}
