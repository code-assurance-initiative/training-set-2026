namespace Shipping.Rates.Core.Domain;

/// <summary>One physical package: actual weight in grams and outer dimensions in centimetres.</summary>
public sealed record Parcel(int WeightGrams, int LengthCm, int WidthCm, int HeightCm, bool IsDangerousGoods = false)
{
    /// <summary>Carriers treat a parcel as oversize when its longest side exceeds 120 cm.</summary>
    public const int OversizeLengthCm = 120;

    public bool IsOversize => Math.Max(LengthCm, Math.Max(WidthCm, HeightCm)) > OversizeLengthCm;

    public int VolumeCubicCm => LengthCm * WidthCm * HeightCm;
}
