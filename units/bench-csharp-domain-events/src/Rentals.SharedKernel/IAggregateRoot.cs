namespace Rentals.SharedKernel;

/// <summary>Marks the entry point of a consistency boundary: the only type a repository loads and saves.</summary>
public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
