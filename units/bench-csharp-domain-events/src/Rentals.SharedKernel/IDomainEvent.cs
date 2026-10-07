namespace Rentals.SharedKernel;

/// <summary>Something that happened inside an aggregate, recorded by the aggregate itself.</summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
