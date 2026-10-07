namespace ClinicScheduling.Domain.Policies;

public sealed record CancellationPolicyOptions
{
    /// <summary>A patient may cancel free of charge up to this long before the appointment starts.</summary>
    public TimeSpan FreeCancellationWindow { get; init; } = TimeSpan.FromHours(24);

    public decimal LateCancellationFee { get; init; } = 250m;

    public decimal NoShowFee { get; init; } = 400m;

    /// <summary>After this many strikes within <see cref="StrikeWindow"/> a deposit is required for new bookings.</summary>
    public int StrikesBeforeDeposit { get; init; } = 2;

    public TimeSpan StrikeWindow { get; init; } = TimeSpan.FromDays(180);

    /// <summary>Video visits are cheaper to lose, so their fees are reduced by this factor.</summary>
    public decimal TelehealthFeeFactor { get; init; } = 0.5m;
}
