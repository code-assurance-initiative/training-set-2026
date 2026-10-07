namespace Rentals.Messaging;

/// <summary>Answers one query type without changing state.</summary>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : notnull
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
