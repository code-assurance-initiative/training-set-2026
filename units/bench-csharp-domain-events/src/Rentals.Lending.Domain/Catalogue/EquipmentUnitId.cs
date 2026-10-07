namespace Rentals.Lending.Domain.Catalogue;

public readonly record struct EquipmentUnitId(Guid Value)
{
    public static EquipmentUnitId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("N");
}
