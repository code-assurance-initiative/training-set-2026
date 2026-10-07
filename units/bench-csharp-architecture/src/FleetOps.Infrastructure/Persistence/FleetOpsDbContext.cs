using FleetOps.Domain.Common;
using FleetOps.Domain.Inspections;
using FleetOps.Domain.Vehicles;
using FleetOps.Domain.WorkOrders;
using FleetOps.Infrastructure.Auditing;
using FleetOps.Infrastructure.FuelCards;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FleetOps.Infrastructure.Persistence;

public sealed class FleetOpsDbContext(DbContextOptions<FleetOpsDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();

    public DbSet<Inspection> Inspections => Set<Inspection>();

    public DbSet<FuelTransaction> FuelTransactions => Set<FuelTransaction>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FleetOpsDbContext).Assembly);

    /// <summary>SQLite cannot compare or sum DateTimeOffset and decimal columns, so both are stored as numbers.</summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
        configurationBuilder.Properties<decimal>().HaveConversion<double>();
    }
}
