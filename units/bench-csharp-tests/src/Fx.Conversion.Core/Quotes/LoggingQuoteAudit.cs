using Microsoft.Extensions.Logging;

namespace Fx.Conversion.Quotes;

/// <summary>Writes the audit record as a structured log event; the log pipeline ships it to the audit store.</summary>
public sealed partial class LoggingQuoteAudit(ILogger<LoggingQuoteAudit> logger) : IQuoteAudit
{
    public void Issued(Quote quote)
    {
        ArgumentNullException.ThrowIfNull(quote);
        LogIssued(quote.Id, quote.Source.Amount, quote.Source.Currency.Code, quote.Target.Amount, quote.Target.Currency.Code, quote.Rate, quote.RatesOf);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Quote {QuoteId} issued: {SourceAmount} {SourceCurrency} -> {TargetAmount} {TargetCurrency} at {Rate} (rates of {RatesOf})")]
    private partial void LogIssued(Guid quoteId, decimal sourceAmount, string sourceCurrency, decimal targetAmount, string targetCurrency, decimal rate, DateOnly ratesOf);
}
