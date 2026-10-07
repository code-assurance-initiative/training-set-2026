namespace Fx.Conversion.Quotes;

public interface IQuoteStore
{
    Task SaveAsync(Quote quote, CancellationToken cancellationToken);

    Task<Quote?> FindAsync(Guid id, CancellationToken cancellationToken);
}
