using Fx.Conversion.Quotes;

namespace Fx.Conversion.Api.Contracts;

public sealed record QuoteResponse(Guid Id, MoneyDto Debit, MoneyDto Credit, MoneyDto Fee, decimal Rate, DateOnly RatesOf, DateTimeOffset ExpiresAt)
{
    public static QuoteResponse From(Quote quote)
    {
        ArgumentNullException.ThrowIfNull(quote);
        return new(quote.Id, MoneyDto.From(quote.Source), MoneyDto.From(quote.Target), MoneyDto.From(quote.Fee), quote.Rate, quote.RatesOf, quote.ExpiresAt);
    }
}
