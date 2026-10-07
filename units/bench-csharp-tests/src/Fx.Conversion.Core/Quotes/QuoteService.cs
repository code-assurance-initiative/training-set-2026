using Fx.Conversion.Currencies;
using Fx.Conversion.Monetary;
using Fx.Conversion.Rates;
using Fx.Conversion.Rounding;

namespace Fx.Conversion.Quotes;

/// <summary>Issues firm quotes: the converted amount at today's rate, net of the fee, valid for a short window.</summary>
public sealed class QuoteService(
    IRateSource rates,
    ICurrencyCatalog catalog,
    IFeePolicy fees,
    IQuoteStore store,
    IQuoteAudit audit,
    IConversionMetrics metrics,
    TimeProvider clock)
{
    public static readonly TimeSpan Validity = TimeSpan.FromSeconds(90);

    public async Task<Quote> CreateAsync(Money source, string targetCode, CancellationToken cancellationToken)
    {
        if (source.Amount <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(source), source, "Only positive amounts can be quoted.");
        }

        var target = catalog.GetByCode(targetCode);
        var table = await rates.GetLatestAsync(cancellationToken).ConfigureAwait(false);
        var rate = table.RateFor(source.Currency.Code, target.Code);
        var fee = fees.FeeFor(source);
        if (fee.Amount >= source.Amount)
        {
            throw new ArgumentOutOfRangeException(nameof(source), source, $"The amount does not cover the fee of {fee}.");
        }

        var net = source.Minus(fee);
        var converted = MoneyRounding.Round(new Money(net.Amount * rate, target), RoundingMode.Down);

        var quote = new Quote(Guid.NewGuid(), source, converted, rate, fee, table.PublishedOn, clock.GetUtcNow() + Validity);
        await store.SaveAsync(quote, cancellationToken).ConfigureAwait(false);
        audit.Issued(quote);
        metrics.QuoteIssued(source.Currency.Code, target.Code);
        return quote;
    }

    /// <summary>The quote, if it exists and has not expired.</summary>
    public async Task<Quote?> FindValidAsync(Guid id, CancellationToken cancellationToken)
    {
        var quote = await store.FindAsync(id, cancellationToken).ConfigureAwait(false);
        return quote is not null && !quote.IsExpired(clock) ? quote : null;
    }
}
