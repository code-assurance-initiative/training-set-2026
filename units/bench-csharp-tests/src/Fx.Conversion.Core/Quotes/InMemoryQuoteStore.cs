using System.Collections.Concurrent;

namespace Fx.Conversion.Quotes;

/// <summary>Quotes live for minutes and are re-creatable, so a process-local store is enough for one instance.</summary>
public sealed class InMemoryQuoteStore : IQuoteStore
{
    private readonly ConcurrentDictionary<Guid, Quote> _quotes = new();

    public Task SaveAsync(Quote quote, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(quote);
        cancellationToken.ThrowIfCancellationRequested();
        _quotes[quote.Id] = quote;
        return Task.CompletedTask;
    }

    public Task<Quote?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_quotes.TryGetValue(id, out var quote) ? quote : null);
    }
}
