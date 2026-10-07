using System.Globalization;
using Fx.Conversion.Allocation;
using Fx.Conversion.Currencies;
using Fx.Conversion.Monetary;
using Fx.Conversion.Rates;
using Fx.Conversion.Rounding;
using Fx.Conversion.TestSupport;
using Reqnroll;
using Xunit;

namespace Fx.Conversion.Specs.Steps;

[Binding]
public sealed class ConversionSteps
{
    private readonly CurrencyCatalog _catalog = new();
    private RateTable? _rates;
    private Money? _result;
    private IReadOnlyList<Money> _parts = [];

    [Given("the reference rates of {word}")]
    public void GivenTheReferenceRatesOf(string publishedOn, DataTable rates)
    {
        ArgumentNullException.ThrowIfNull(rates);
        var date = DateOnly.ParseExact(publishedOn, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        _rates = new RateTable("EUR", date, rates.Rows.ToDictionary(
            row => row["currency"],
            row => decimal.Parse(row["per euro"], CultureInfo.InvariantCulture),
            StringComparer.Ordinal));
    }

    [When("{decimal} {word} is converted to {word}")]
    public async Task WhenConverted(decimal amount, string from, string to)
    {
        var rates = _rates ?? throw new InvalidOperationException("No reference rates were given.");
        var converter = new CurrencyConverter(new FixedRateSource(rates), _catalog);
        _result = await converter.ConvertAsync(new Money(amount, _catalog.GetByCode(from)), to, RoundingMode.HalfEven, CancellationToken.None);
    }

    [When("{decimal} {word} is split into {int} equal parts")]
    public void WhenSplit(decimal amount, string currency, int parts)
    {
        _parts = Allocator.Split(new Money(amount, _catalog.GetByCode(currency)), parts);
    }

    [Then("the result is {decimal} {word}")]
    public void ThenTheResultIs(decimal amount, string currency)
    {
        Assert.NotNull(_result);
        MoneyAssert.Equal(amount, currency, _result.Value);
    }

    [Then("the parts are {decimal}, {decimal} and {decimal} {word}")]
    public void ThenThePartsAre(decimal first, decimal second, decimal third, string currency)
    {
        Assert.Equal([first, second, third], _parts.Select(part => part.Amount));
        Assert.All(_parts, part => Assert.Equal(currency, part.Currency.Code));
    }
}
