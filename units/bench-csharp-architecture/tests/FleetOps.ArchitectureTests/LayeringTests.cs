using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace FleetOps.ArchitectureTests;

/// <summary>ADR 0002 — layered architecture: dependencies point inward.</summary>
public sealed class LayeringTests
{
    [Fact]
    public void Adr0002DomainDoesNotDependOnOuterLayers() =>
        FleetArchitecture.Check(
            Types().That().ResideInAssembly(FleetArchitecture.Domain)
                .Should().NotDependOnAny(Types().That().ResideInAssembly(
                    FleetArchitecture.Application, FleetArchitecture.Infrastructure, FleetArchitecture.Api, FleetArchitecture.Worker)));

    [Fact]
    public void Adr0002DomainDoesNotUseEntityFramework() =>
        FleetArchitecture.Check(
            Types().That().ResideInAssembly(FleetArchitecture.Domain)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^Microsoft\.EntityFrameworkCore(\..*)?$")));

    [Fact]
    public void Adr0002ApplicationDoesNotDependOnInfrastructureOrHosts() =>
        FleetArchitecture.Check(
            Types().That().ResideInAssembly(FleetArchitecture.Application)
                .Should().NotDependOnAny(Types().That().ResideInAssembly(
                    FleetArchitecture.Infrastructure, FleetArchitecture.Api, FleetArchitecture.Worker)));

    [Fact]
    public void Adr0002InfrastructureDoesNotDependOnHosts() =>
        FleetArchitecture.Check(
            Types().That().ResideInAssembly(FleetArchitecture.Infrastructure)
                .Should().NotDependOnAny(Types().That().ResideInAssembly(FleetArchitecture.Api, FleetArchitecture.Worker)));
}
