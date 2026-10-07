using Rentals.Lending.Domain.Catalogue;
using Rentals.Lending.Domain.Maintenance;
using Rentals.SharedKernel;

namespace Rentals.Lending.Tests.Domain;

public sealed class EquipmentTests
{
    private static Equipment Drill() =>
        new(EquipmentId.New(), "Cordless drill", new Money(4m, "EUR"), new Money(180m, "EUR"));

    [Fact]
    public void Constructor_rejects_a_blank_name() =>
        Assert.Throws<ArgumentException>(() => new Equipment(EquipmentId.New(), " ", new Money(1m, "EUR"), new Money(1m, "EUR")));

    [Fact]
    public void AddUnit_rejects_a_duplicate_serial_number()
    {
        var drill = Drill();
        drill.AddUnit(new SerialNumber("dr-001"));

        Assert.Throws<DomainRuleViolationException>(() => drill.AddUnit(new SerialNumber("DR-001")));
    }

    [Fact]
    public void A_damaged_unit_is_not_lendable_until_repaired()
    {
        var drill = Drill();
        var unit = drill.AddUnit(new SerialNumber("DR-001"));

        drill.ReportDamage(unit.Id, UnitCondition.Damaged);
        Assert.False(unit.IsLendable);

        unit.Repair();
        Assert.True(unit.IsLendable);
    }

    [Fact]
    public void A_retired_unit_stays_retired_when_damage_is_reported()
    {
        var drill = Drill();
        var unit = drill.AddUnit(new SerialNumber("DR-001"));
        drill.RetireUnit(unit.Id);

        drill.ReportDamage(unit.Id, UnitCondition.Lost);

        Assert.Equal(UnitCondition.Retired, unit.Condition);
    }

    [Fact]
    public void A_unit_of_another_equipment_is_rejected() =>
        Assert.Throws<DomainRuleViolationException>(() => Drill().Unit(EquipmentUnitId.New()));

    [Fact]
    public void ChangeRates_replaces_both_rates()
    {
        var drill = Drill();

        drill.ChangeRates(new Money(5m, "EUR"), new Money(200m, "EUR"));
        drill.Rename("Cordless drill 18V");

        Assert.Equal(new Money(5m, "EUR"), drill.DailyRate);
        Assert.Equal("Cordless drill 18V", drill.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("has space")]
    [InlineData("0123456789012345678901234567890123456789X")]
    public void SerialNumber_rejects_malformed_values(string value) =>
        Assert.ThrowsAny<ArgumentException>(() => new SerialNumber(value));

    [Fact]
    public void A_maintenance_record_completes_once()
    {
        var record = new MaintenanceRecord(
            MaintenanceRecordId.New(), EquipmentUnitId.New(), "Replace chuck", 12m, DateTimeOffset.UnixEpoch);

        record.Complete(DateTimeOffset.UnixEpoch.AddDays(1));

        Assert.Equal(DateTimeOffset.UnixEpoch.AddDays(1), record.CompletedAt);
        Assert.Throws<DomainRuleViolationException>(() => record.Complete(DateTimeOffset.UnixEpoch.AddDays(2)));
    }

    [Fact]
    public void Relocate_records_the_new_location_and_raises_an_event()
    {
        var drill = Drill();
        var at = new DateTimeOffset(2026, 5, 4, 8, 0, 0, TimeSpan.Zero);

        drill.Relocate(new StorageLocation("Northside", "b-12"), at);

        Assert.Equal("Northside, shelf B-12", drill.Location?.ToString());
        Assert.Equal(new EquipmentRelocated(drill.Id, "Northside", "B-12", at), Assert.Single(drill.DomainEvents));
    }
}
