using Fx.Conversion.Currencies;

namespace Fx.Conversion.TestSupport;

/// <summary>Short-hands for the catalog's currencies in test arrangements.</summary>
public static class KnownCurrencies
{
    private static readonly CurrencyCatalog Catalog = new();

    public static Currency Eur => Catalog.GetByCode("EUR");

    public static Currency Usd => Catalog.GetByCode("USD");

    public static Currency Gbp => Catalog.GetByCode("GBP");

    public static Currency Jpy => Catalog.GetByCode("JPY");
}
