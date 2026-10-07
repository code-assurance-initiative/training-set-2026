namespace Fx.Conversion.Quotes;

public interface IConversionMetrics
{
    void QuoteIssued(string sourceCurrency, string targetCurrency);
}
