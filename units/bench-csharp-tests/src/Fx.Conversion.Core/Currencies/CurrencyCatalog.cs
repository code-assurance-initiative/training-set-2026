using System.Diagnostics.CodeAnalysis;

namespace Fx.Conversion.Currencies;

/// <summary>The fixed ISO 4217 catalog of the currencies the reference-rate feed quotes.</summary>
public sealed class CurrencyCatalog : ICurrencyCatalog
{
    private readonly Dictionary<string, Currency> _byCode;

    public CurrencyCatalog()
    {
        _byCode = Iso4217Table.MinorUnits.ToDictionary(
            pair => pair.Key,
            pair => new Currency(pair.Key, pair.Value),
            StringComparer.Ordinal);
    }

    public IReadOnlyCollection<Currency> All => _byCode.Values;

    public bool TryGet(string code, [NotNullWhen(true)] out Currency? currency)
    {
        currency = null;
        return Iso4217Table.IsWellFormed(code) && _byCode.TryGetValue(code, out currency);
    }

    public Currency GetByCode(string code) =>
        TryGet(code, out var currency) ? currency : throw new UnknownCurrencyException(code);
}
