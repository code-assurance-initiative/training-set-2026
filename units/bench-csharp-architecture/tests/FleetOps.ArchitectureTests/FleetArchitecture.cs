using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;

namespace FleetOps.ArchitectureTests;

/// <summary>The production assemblies, loaded once for every rule.</summary>
public static class FleetArchitecture
{
    public static readonly System.Reflection.Assembly Contracts = typeof(Contracts.ContractVersion).Assembly;
    public static readonly System.Reflection.Assembly Domain = typeof(Domain.Vehicles.Vehicle).Assembly;
    public static readonly System.Reflection.Assembly Application = typeof(Application.ApplicationServiceCollectionExtensions).Assembly;
    public static readonly System.Reflection.Assembly Infrastructure = typeof(Infrastructure.Persistence.FleetOpsDbContext).Assembly;
    public static readonly System.Reflection.Assembly Api = typeof(Program).Assembly;
    public static readonly System.Reflection.Assembly Worker = typeof(Worker.MaintenanceReminderWorker).Assembly;

    public static Architecture Model { get; } = new ArchLoader()
        .LoadAssemblies(
            Contracts,
            Domain,
            Application,
            Infrastructure,
            Api,
            Worker,
            typeof(Infrastructure.Telematics.ITelematicsClient).Assembly,
            typeof(Diagnostics.TelematicsHealthCheck).Assembly,
            typeof(ServiceDefaults.ServiceDefaultsExtensions).Assembly)
        .Build();

    /// <summary>Asserts the rule and prints every violation when it fails.</summary>
    public static void Check(IArchRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        var failures = rule.Evaluate(Model).Where(r => !r.Passed).Select(r => r.Description).ToList();
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }
}
