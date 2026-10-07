using Fx.Conversion.Currencies;
using Fx.Conversion.Monetary;
using Fx.Conversion.Rounding;

namespace Fx.Conversion.Rates;

/// <summary>Converts amounts between currencies at the latest reference rate, rounded to the target's minor unit.</summary>
public sealed class CurrencyConverter(IRateSource rates, ICurrencyCatalog catalog)
{
    public async Task<Money> ConvertAsync(Money amount, string targetCode, RoundingMode rounding, CancellationToken cancellationToken)
    {
        var target = catalog.GetByCode(targetCode);
        var table = await rates.GetLatestAsync(cancellationToken).ConfigureAwait(false);
        var rate = table.RateFor(amount.Currency.Code, target.Code);
        return MoneyRounding.Round(new Money(amount.Amount * rate, target), rounding);
    }
}
