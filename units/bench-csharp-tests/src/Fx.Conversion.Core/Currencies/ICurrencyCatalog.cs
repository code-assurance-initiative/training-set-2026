namespace Fx.Conversion.Currencies;

/// <summary>The currencies this service converts between.</summary>
public interface ICurrencyCatalog
{
    IReadOnlyCollection<Currency> All { get; }

    bool TryGet(string code, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Currency? currency);

    /// <summary>The currency for <paramref name="code"/>; throws <see cref="UnknownCurrencyException"/> if there is none.</summary>
    Currency GetByCode(string code);
}
