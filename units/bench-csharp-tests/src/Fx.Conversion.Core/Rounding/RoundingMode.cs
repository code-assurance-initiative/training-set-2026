namespace Fx.Conversion.Rounding;

/// <summary>How an amount is brought to its currency's minor unit.</summary>
public enum RoundingMode
{
    /// <summary>To the nearest unit; ties to the even neighbour (banker's rounding). The default for conversions.</summary>
    HalfEven,

    /// <summary>To the nearest unit; ties away from zero (commercial rounding).</summary>
    HalfUp,

    /// <summary>Toward zero (truncation).</summary>
    Down,

    /// <summary>Away from zero.</summary>
    Up,

    /// <summary>Toward negative infinity.</summary>
    Floor,

    /// <summary>Toward positive infinity.</summary>
    Ceiling,
}
