using Rentals.Lending.Application.Catalogue;
using Rentals.Lending.Domain.Catalogue;

namespace Rentals.Lending.Tests.Application;

public sealed class CatalogueHandlerTests
{
    [Fact]
    public async Task Relocating_equipment_stores_the_branch_and_shelf_it_is_collected_from()
    {
        await using var lending = await LendingHarness.StartAsync();
        var drill = await lending.RegisterDrillAsync();

        await lending.SendAsync(new RelocateEquipmentCommand(drill.Id, "Northside", "b-12"));

        var stored = await lending.InScopeAsync<IEquipmentRepository, Equipment?>(equipment =>
            equipment.GetAsync(drill.Id, TestContext.Current.CancellationToken));
        Assert.Equal(("Northside", "B-12"), (stored?.Location?.Branch, stored?.Location?.Shelf));
    }
}
