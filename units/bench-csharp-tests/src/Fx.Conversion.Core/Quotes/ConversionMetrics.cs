using System.Diagnostics.Metrics;

namespace Fx.Conversion.Quotes;

/// <summary>OpenTelemetry-compatible counters on the <c>Fx.Conversion</c> meter.</summary>
public sealed class ConversionMetrics : IConversionMetrics, IDisposable
{
    public const string MeterName = "Fx.Conversion";

    private readonly Meter _meter;
    private readonly Counter<long> _quotes;

    public ConversionMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);
        _meter = meterFactory.Create(MeterName);
        _quotes = _meter.CreateCounter<long>("fx.quotes.issued", description: "Quotes issued, by currency pair.");
    }

    public void QuoteIssued(string sourceCurrency, string targetCurrency) =>
        _quotes.Add(1, new KeyValuePair<string, object?>("source", sourceCurrency), new KeyValuePair<string, object?>("target", targetCurrency));

    public void Dispose() => _meter.Dispose();
}
