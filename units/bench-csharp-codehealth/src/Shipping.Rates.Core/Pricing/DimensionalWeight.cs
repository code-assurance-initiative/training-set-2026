using Shipping.Rates.Core.Domain;

namespace Shipping.Rates.Core.Pricing;

/// <summary>
/// Carriers charge the greater of actual and volumetric weight. Volumetric weight in kilograms is the volume in
/// cubic centimetres divided by the carrier's divisor (commonly 5000).
/// </summary>
public static class DimensionalWeight
{
    public static int VolumetricGrams(Parcel parcel, int divisor)
    {
        ArgumentNullException.ThrowIfNull(parcel);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(divisor);
        return (int)Math.Ceiling(parcel.VolumeCubicCm * 1000m / divisor);
    }

    public static int ChargeableGrams(Parcel parcel, int divisor) =>
        Math.Max(parcel?.WeightGrams ?? throw new ArgumentNullException(nameof(parcel)), VolumetricGrams(parcel, divisor));

    /// <summary>Tariffs are priced per started half kilogram.</summary>
    public static decimal ChargeableKilograms(int grams)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(grams);
        return Math.Ceiling(grams / 500m) / 2m;
    }
}
