using ParcelTracking.Core.Parcels;

namespace ParcelTracking.UnitTests.Parcels;

public sealed class StatusTransitionsTests
{
    [Theory]
    [InlineData(ParcelStatus.Registered, ParcelStatus.InTransit)]
    [InlineData(ParcelStatus.InTransit, ParcelStatus.OutForDelivery)]
    [InlineData(ParcelStatus.OutForDelivery, ParcelStatus.Delivered)]
    [InlineData(ParcelStatus.OutForDelivery, ParcelStatus.DeliveryFailed)]
    [InlineData(ParcelStatus.DeliveryFailed, ParcelStatus.OutForDelivery)]
    [InlineData(ParcelStatus.InTransit, ParcelStatus.HeldAtPickupPoint)]
    [InlineData(ParcelStatus.InTransit, ParcelStatus.Returned)]
    public void Forward_moves_are_allowed(ParcelStatus from, ParcelStatus to) =>
        Assert.True(StatusTransitions.CanMove(from, to));

    [Theory]
    [InlineData(ParcelStatus.OutForDelivery, ParcelStatus.InTransit)]
    [InlineData(ParcelStatus.InTransit, ParcelStatus.Registered)]
    [InlineData(ParcelStatus.InTransit, ParcelStatus.DeliveryFailed)]
    [InlineData(ParcelStatus.Registered, ParcelStatus.Returned)]
    [InlineData(ParcelStatus.InTransit, ParcelStatus.InTransit)]
    public void Backward_or_impossible_moves_are_rejected(ParcelStatus from, ParcelStatus to) =>
        Assert.False(StatusTransitions.CanMove(from, to));

    [Theory]
    [InlineData(ParcelStatus.Delivered)]
    [InlineData(ParcelStatus.Returned)]
    public void Terminal_statuses_never_move(ParcelStatus terminal)
    {
        Assert.True(StatusTransitions.IsTerminal(terminal));
        foreach (var to in Enum.GetValues<ParcelStatus>())
        {
            Assert.False(StatusTransitions.CanMove(terminal, to));
        }
    }
}
