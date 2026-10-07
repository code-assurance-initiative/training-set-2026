using Rentals.Lending.Domain.Catalogue;
using Rentals.SharedKernel;

namespace Rentals.Lending.Tests.Domain;

public sealed class EntityTests
{
    [Fact]
    public void Entities_are_equal_by_type_and_id()
    {
        var id = EquipmentUnitId.New();
        var a = new EquipmentUnit(id, new SerialNumber("DR-001"));
        var b = new EquipmentUnit(id, new SerialNumber("DR-999"));
        var c = new EquipmentUnit(EquipmentUnitId.New(), new SerialNumber("DR-001"));

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, c);
        Assert.False(a.Equals((object?)null));
    }

    [Fact]
    public void A_rule_violation_carries_its_message_and_cause()
    {
        var cause = new InvalidOperationException("inner");

        var error = new DomainRuleViolationException("outer", cause);

        Assert.Equal("outer", error.Message);
        Assert.Same(cause, error.InnerException);
    }
}
