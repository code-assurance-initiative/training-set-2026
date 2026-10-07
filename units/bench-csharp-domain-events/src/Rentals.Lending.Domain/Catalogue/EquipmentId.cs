namespace Rentals.Lending.Domain.Catalogue;

public readonly record struct EquipmentId(Guid Value)
{
    public static EquipmentId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("N");
}
