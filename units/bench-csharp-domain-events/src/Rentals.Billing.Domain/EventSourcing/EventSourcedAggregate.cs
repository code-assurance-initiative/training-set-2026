using Rentals.SharedKernel;

namespace Rentals.Billing.Domain.EventSourcing;

/// <summary>
/// An aggregate whose state is the fold of its events. Commands decide and <see cref="Emit"/> events; <see cref="Apply"/>
/// applies one event to the state and must depend on nothing but the event, so that replay reproduces the state.
/// </summary>
public abstract class EventSourcedAggregate<TId> : AggregateRoot<TId>
    where TId : struct, IEquatable<TId>
{
    protected EventSourcedAggregate(TId id)
        : base(id)
    {
    }

    /// <summary>The number of events in the stream as loaded, before any event emitted since.</summary>
    public long Version { get; private set; }

    public void LoadFrom(IEnumerable<IDomainEvent> history)
    {
        ArgumentNullException.ThrowIfNull(history);
        foreach (var domainEvent in history)
        {
            Apply(domainEvent);
            Version++;
        }
    }

    public void MarkCommitted()
    {
        Version += DomainEvents.Count;
        ClearDomainEvents();
    }

    protected void Emit(IDomainEvent domainEvent)
    {
        Apply(domainEvent);
        Raise(domainEvent);
    }

    protected abstract void Apply(IDomainEvent domainEvent);
}
