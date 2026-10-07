using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Rentals.Lending.Domain.Catalogue;
using Rentals.Lending.Domain.Loans;
using Rentals.Lending.Domain.Maintenance;
using Rentals.Lending.Domain.Members;
using Rentals.Lending.Domain.Reservations;
using Rentals.Lending.Infrastructure.Messaging;
using Rentals.SharedKernel;

namespace Rentals.Lending.Infrastructure.Persistence;

/// <summary>
/// The Lending database. Saving also writes the integration events raised by the saved aggregates to the outbox,
/// in the same transaction, so a committed change and the messages it produced are never separated.
/// </summary>
public sealed class LendingDbContext(DbContextOptions<LendingDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Member> Members => Set<Member>();

    public DbSet<Equipment> Equipment => Set<Equipment>();

    public DbSet<Loan> Loans => Set<Loan>();

    public DbSet<Reservation> Reservations => Set<Reservation>();

    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();

    internal DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) =>
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker.Entries<IAggregateRoot>()
            .Select(e => e.Entity)
            .Where(a => a.DomainEvents.Count > 0)
            .ToList();
        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                foreach (var integrationEvent in IntegrationEventMapper.Map(aggregate, domainEvent))
                {
                    OutboxMessages.Add(IntegrationEventSerializer.ToOutbox(integrationEvent));
                }
            }

            aggregate.ClearDomainEvents();
        }

        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken).ConfigureAwait(false);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        // SQLite has no native date-time-offset type; the binary form keeps ordering and comparison translatable.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LendingDbContext).Assembly);
}
