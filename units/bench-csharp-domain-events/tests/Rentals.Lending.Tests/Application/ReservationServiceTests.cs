using Rentals.Lending.Application.Reservations;
using Rentals.Lending.Domain.Reservations;
using Rentals.SharedKernel;

namespace Rentals.Lending.Tests.Application;

public sealed class ReservationServiceTests
{
    [Fact]
    public async Task A_reservation_is_placed_confirmed_and_cancelled()
    {
        await using var lending = await LendingHarness.StartAsync();
        var member = await lending.RegisterMemberAsync();
        var drill = await lending.RegisterDrillAsync();
        var from = LendingHarness.Now.AddDays(2);

        var id = await lending.InScopeAsync<ReservationService, ReservationId>(service =>
            service.ReserveAsync(member.Id, drill.Id, from, from.AddDays(3), TestContext.Current.CancellationToken));
        await lending.InScopeAsync<ReservationService>(service => service.ConfirmAsync(id, TestContext.Current.CancellationToken));

        var confirmed = await lending.InScopeAsync<IReservationRepository, Reservation?>(r =>
            r.GetAsync(id, TestContext.Current.CancellationToken));
        Assert.Equal(ReservationStatus.Confirmed, confirmed?.Status);

        await lending.InScopeAsync<ReservationService>(service => service.CancelAsync(id, TestContext.Current.CancellationToken));
        var cancelled = await lending.InScopeAsync<IReservationRepository, Reservation?>(r =>
            r.GetAsync(id, TestContext.Current.CancellationToken));
        Assert.Equal(ReservationStatus.Cancelled, cancelled?.Status);
    }

    [Fact]
    public async Task Overlapping_reservations_cannot_exceed_the_units()
    {
        await using var lending = await LendingHarness.StartAsync();
        var ada = await lending.RegisterMemberAsync();
        var grace = await lending.RegisterMemberAsync("grace@example.org");
        var drill = await lending.RegisterDrillAsync();
        var from = LendingHarness.Now.AddDays(2);
        await lending.InScopeAsync<ReservationService, ReservationId>(service =>
            service.ReserveAsync(ada.Id, drill.Id, from, from.AddDays(3), TestContext.Current.CancellationToken));

        await Assert.ThrowsAsync<DomainRuleViolationException>(() =>
            lending.InScopeAsync<ReservationService, ReservationId>(service =>
                service.ReserveAsync(grace.Id, drill.Id, from.AddDays(1), from.AddDays(2), TestContext.Current.CancellationToken)));
    }

    [Fact]
    public async Task A_reservation_cannot_start_in_the_past()
    {
        await using var lending = await LendingHarness.StartAsync();
        var member = await lending.RegisterMemberAsync();
        var drill = await lending.RegisterDrillAsync();

        await Assert.ThrowsAsync<DomainRuleViolationException>(() =>
            lending.InScopeAsync<ReservationService, ReservationId>(service =>
                service.ReserveAsync(member.Id, drill.Id, LendingHarness.Now.AddDays(-1), LendingHarness.Now.AddDays(1),
                    TestContext.Current.CancellationToken)));
    }
}
