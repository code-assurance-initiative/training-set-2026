using Fx.Conversion.Currencies;

namespace Fx.Conversion.Rates;

/// <summary>
/// One day's reference rates: how many units of each quoted currency one unit of the base currency buys. Rates
/// between two quoted currencies are derived through the base (cross rates).
/// </summary>
public sealed class RateTable
{
    private readonly Dictionary<string, decimal> _rates;

    public RateTable(string baseCurrency, DateOnly publishedOn, IReadOnlyDictionary<string, decimal> rates)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseCurrency);
        ArgumentNullException.ThrowIfNull(rates);
        if (rates.Values.Any(rate => rate <= 0m))
        {
            throw new ArgumentException("Rates must be positive.", nameof(rates));
        }

        BaseCurrency = baseCurrency;
        PublishedOn = publishedOn;
        _rates = new Dictionary<string, decimal>(rates, StringComparer.Ordinal) { [baseCurrency] = 1m };
    }

    public string BaseCurrency { get; }

    public DateOnly PublishedOn { get; }

    public IReadOnlyDictionary<string, decimal> Rates => _rates;

    public bool Quotes(string currencyCode) => _rates.ContainsKey(currencyCode);

    /// <summary>Units of <paramref name="to"/> for one unit of <paramref name="from"/>.</summary>
    public decimal RateFor(string from, string to)
    {
        var perBaseFrom = Lookup(from);
        var perBaseTo = Lookup(to);
        return string.Equals(from, to, StringComparison.Ordinal) ? 1m : perBaseTo / perBaseFrom;
    }

    /// <summary>Whole days since publication, counted in UTC (the feed publishes by the UTC calendar day).</summary>
    public int AgeInDays(TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        return today.DayNumber - PublishedOn.DayNumber;
    }

    private decimal Lookup(string code) =>
        _rates.TryGetValue(code, out var rate) ? rate : throw new UnknownCurrencyException(code);
}
