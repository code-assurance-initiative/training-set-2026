namespace Rentals.SharedKernel;

/// <summary>An entity that owns a consistency boundary and records the domain events it raises.</summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
    where TId : struct, IEquatable<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
