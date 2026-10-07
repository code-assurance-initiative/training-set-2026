using System.Collections.Concurrent;

namespace FleetOps.Infrastructure.Tyres;

/// <summary>The cheapest valid quote per tyre size and season, shared by every request.</summary>
public sealed class TyrePriceCache(TimeProvider clock)
{
    private readonly ConcurrentDictionary<(TyreSize Size, TyreSeason Season), TyreQuote> _best = new();

    public void Offer(TyreQuote quote)
    {
        ArgumentNullException.ThrowIfNull(quote);
        _best.AddOrUpdate(
            (quote.Size, quote.Season),
            quote,
            (_, current) => quote.UnitPrice < current.UnitPrice || !IsValid(current) ? quote : current);
    }

    public TyreQuote? Best(TyreSize size, TyreSeason season) =>
        _best.TryGetValue((size, season), out var quote) && IsValid(quote) ? quote : null;

    private bool IsValid(TyreQuote quote) => quote.ValidUntil >= DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
}
