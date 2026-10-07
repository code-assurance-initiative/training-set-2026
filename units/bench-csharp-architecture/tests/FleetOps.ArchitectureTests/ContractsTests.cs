using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace FleetOps.ArchitectureTests;

/// <summary>ADR 0005 — the wire contracts depend on no other FleetOps assembly.</summary>
public sealed class ContractsTests
{
    [Fact]
    public void Adr0005ContractsDependOnNoOtherFleetopsAssembly() =>
        FleetArchitecture.Check(
            Types().That().ResideInAssembly(FleetArchitecture.Contracts)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^FleetOps\.(?!Contracts(\.|$)).*")));

    [Fact]
    public void Adr0005ContractsAreRecordsOrConstants() =>
        FleetArchitecture.Check(
            Classes().That().ResideInAssembly(FleetArchitecture.Contracts)
                .Should().BeRecord().OrShould().BeAbstract());
}
