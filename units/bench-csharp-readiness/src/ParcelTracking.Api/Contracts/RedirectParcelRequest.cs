namespace ParcelTracking.Api.Contracts;

public sealed class RedirectParcelRequest
{
    public string PickupPointId { get; init; } = string.Empty;

    public DateOnly? HoldUntil { get; init; }

    public string? Note { get; init; }
}
