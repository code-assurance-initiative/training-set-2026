using Rentals.Lending.Domain.Catalogue;
using Rentals.SharedKernel;

namespace Rentals.Lending.Domain.Maintenance;

/// <summary>A service, inspection or repair carried out on one unit.</summary>
public sealed class MaintenanceRecord : Entity<MaintenanceRecordId>
{
    private DateTimeOffset? _completedAt;

    public MaintenanceRecord(MaintenanceRecordId id, EquipmentUnitId unitId, string description, decimal estimatedCost, DateTimeOffset openedAt)
        : base(id)
    {
        UnitId = unitId;
        Description = description;
        EstimatedCost = estimatedCost;
        OpenedAt = openedAt;
    }

    public EquipmentUnitId UnitId { get; }

    public string Description { get; }

    public decimal EstimatedCost { get; }

    public DateTimeOffset OpenedAt { get; }

    public DateTimeOffset? CompletedAt => _completedAt;

    public void Complete(DateTimeOffset completedAt)
    {
        if (_completedAt is not null)
        {
            throw new DomainRuleViolationException("This maintenance record is already completed.");
        }

        _completedAt = completedAt;
    }
}
