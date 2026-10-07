namespace Fx.Conversion.Currencies;

/// <summary>A currency code that is not in the catalog (or not a currency at all).</summary>
public sealed class UnknownCurrencyException : Exception
{
    public UnknownCurrencyException(string code)
        : base($"Unknown currency '{code}'.")
    {
        Code = code;
    }

    public string? Code { get; }
}
