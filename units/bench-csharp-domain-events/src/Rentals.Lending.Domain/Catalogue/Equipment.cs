using Rentals.SharedKernel;

namespace Rentals.Lending.Domain.Catalogue;

/// <summary>A type of equipment the library lends (a model of drill, a ladder), with the physical units it owns.</summary>
public sealed class Equipment : AggregateRoot<EquipmentId>
{
    private readonly List<EquipmentUnit> _units = [];

    public Equipment(EquipmentId id, string name, Money dailyRate, Money replacementValue)
        : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(dailyRate);
        ArgumentNullException.ThrowIfNull(replacementValue);
        Name = name.Trim();
        DailyRate = dailyRate;
        ReplacementValue = replacementValue;
    }

    private Equipment(EquipmentId id, string name)
        : base(id)
    {
        Name = name;
        DailyRate = Money.Zero("EUR");
        ReplacementValue = Money.Zero("EUR");
    }

    public string Name { get; private set; }

    public Money DailyRate { get; private set; }

    public Money ReplacementValue { get; private set; }

    public StorageLocation? Location { get; private set; }

    public IReadOnlyCollection<EquipmentUnit> Units => _units.AsReadOnly();

    public EquipmentUnit AddUnit(SerialNumber serialNumber)
    {
        ArgumentNullException.ThrowIfNull(serialNumber);
        if (_units.Exists(u => u.SerialNumber == serialNumber))
        {
            throw new DomainRuleViolationException($"Unit {serialNumber} is already registered for {Name}.");
        }

        var unit = new EquipmentUnit(EquipmentUnitId.New(), serialNumber);
        _units.Add(unit);
        return unit;
    }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }

    public void ChangeRates(Money dailyRate, Money replacementValue)
    {
        ArgumentNullException.ThrowIfNull(dailyRate);
        ArgumentNullException.ThrowIfNull(replacementValue);
        DailyRate = dailyRate;
        ReplacementValue = replacementValue;
    }

    public void Relocate(StorageLocation location, DateTimeOffset relocatedAt)
    {
        ArgumentNullException.ThrowIfNull(location);
        if (Location == location)
        {
            return;
        }

        Location = location;
        Raise(new EquipmentRelocated(Id, location.Branch, location.Shelf, relocatedAt));
    }

    public EquipmentUnit Unit(EquipmentUnitId unitId) =>
        _units.Find(u => u.Id == unitId)
        ?? throw new DomainRuleViolationException($"Unit {unitId} does not belong to {Name}.");

    public void ReportDamage(EquipmentUnitId unitId, UnitCondition condition) => Unit(unitId).ReportDamage(condition);

    public void RetireUnit(EquipmentUnitId unitId) => Unit(unitId).Retire();
}
