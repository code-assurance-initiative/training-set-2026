using FleetOps.Domain.Inspections;
using FleetOps.Domain.Vehicles;
using FleetOps.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FleetOps.Infrastructure.Auditing;

/// <summary>Adds an <see cref="AuditEntry"/> for every vehicle, work order and inspection a save changes.</summary>
public sealed class AuditSaveChangesInterceptor(TimeProvider clock) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AddAuditEntries(eventData);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        AddAuditEntries(eventData);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddAuditEntries(DbContextEventData eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        if (eventData.Context is { } context)
        {
            var now = clock.GetUtcNow();
            var entries = context.ChangeTracker.Entries()
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .Select(e => (e.State, Id: IdOf(e.Entity)))
                .ToList();
            foreach (var (state, entity) in entries)
            {
                if (entity is not { } id)
                {
                    continue;
                }

                context.Add(new AuditEntry
                {
                    Id = Guid.NewGuid(),
                    EntityType = id.Type,
                    EntityId = id.Id,
                    Action = state switch
                    {
                        EntityState.Added => AuditAction.Created,
                        EntityState.Deleted => AuditAction.Deleted,
                        _ => AuditAction.Modified,
                    },
                    At = now,
                });
            }
        }
    }

    private static (string Type, string Id)? IdOf(object entity) => entity switch
    {
        Vehicle v => (nameof(Vehicle), v.Id.Value.ToString()),
        WorkOrder w => (nameof(WorkOrder), w.Id.Value.ToString()),
        Inspection i => (nameof(Inspection), i.Id.Value.ToString()),
        _ => null,
    };
}
