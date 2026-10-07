namespace Rentals.SharedKernel;

/// <summary>Commits the changes of one operation atomically, together with the messages they produced.</summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
