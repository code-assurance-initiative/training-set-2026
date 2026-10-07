namespace Fx.Conversion.Currencies;

/// <summary>An ISO 4217 currency: its alphabetic code and the number of minor-unit digits it is quoted in.</summary>
public sealed record Currency
{
    public Currency(string code, int minorUnits)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentOutOfRangeException.ThrowIfNegative(minorUnits);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minorUnits, 4);
        Code = code;
        MinorUnits = minorUnits;
    }

    public string Code { get; }

    public int MinorUnits { get; }

    /// <summary>The value of one minor unit: 0.01 for EUR, 1 for JPY, 0.001 for KWD.</summary>
    public decimal MinorUnit => MinorUnits switch
    {
        0 => 1m,
        1 => 0.1m,
        2 => 0.01m,
        3 => 0.001m,
        _ => 0.0001m,
    };

    public override string ToString() => Code;
}
