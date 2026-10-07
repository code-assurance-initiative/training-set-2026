using Rentals.SharedKernel;

namespace Rentals.Lending.Domain.Catalogue;

/// <summary>One physical, serial-numbered copy of an <see cref="Equipment"/> type. Reached only through its equipment.</summary>
public sealed class EquipmentUnit : Entity<EquipmentUnitId>
{
    public EquipmentUnit(EquipmentUnitId id, SerialNumber serialNumber)
        : base(id)
    {
        SerialNumber = serialNumber;
        Condition = UnitCondition.Good;
    }

    public SerialNumber SerialNumber { get; private set; }

    public UnitCondition Condition { get; private set; }

    public bool IsLendable => Condition == UnitCondition.Good;

    public void ReportDamage(UnitCondition condition)
    {
        if (condition is not (UnitCondition.Damaged or UnitCondition.Lost))
        {
            throw new ArgumentOutOfRangeException(nameof(condition), condition, "Only damage or loss can be reported.");
        }

        if (Condition == UnitCondition.Retired)
        {
            return;
        }

        Condition = condition;
    }

    public void Repair()
    {
        if (Condition != UnitCondition.Damaged)
        {
            throw new DomainRuleViolationException($"Unit {SerialNumber} is {Condition}, not damaged.");
        }

        Condition = UnitCondition.Good;
    }

    public void Retire() => Condition = UnitCondition.Retired;
}
