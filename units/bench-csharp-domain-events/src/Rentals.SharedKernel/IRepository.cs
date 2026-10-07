namespace Rentals.SharedKernel;

/// <summary>Loads and stores whole entities by identity. Changes are committed by <see cref="IUnitOfWork"/>.</summary>
public interface IRepository<TEntity, in TId>
    where TEntity : Entity<TId>
    where TId : struct, IEquatable<TId>
{
    Task<TEntity?> GetAsync(TId id, CancellationToken cancellationToken);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken);

    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken);
}
