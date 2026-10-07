namespace FleetOps.Domain.Common;

/// <summary>Commits every change tracked by the repositories in one transaction.</summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
