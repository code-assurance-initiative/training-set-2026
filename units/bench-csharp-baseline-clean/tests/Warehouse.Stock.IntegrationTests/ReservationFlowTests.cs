using System.Net;
using System.Net.Http.Json;
using Warehouse.Stock.Api.Contracts;
using Warehouse.Stock.Api.Security;

namespace Warehouse.Stock.IntegrationTests;

/// <summary>Each test gets its own host, because the flows change shared stock and move the shared clock.</summary>
public sealed class ReservationFlowTests : IDisposable
{
    private static readonly string StockPath = $"api/stock/{ApiClientExtensions.SkuCode}";

    private readonly StockApiFactory _factory = new();
    private readonly HttpClient _client;

    public ReservationFlowTests()
    {
        _client = _factory.CreateClient(
            AuthorizationPolicies.StockRead, AuthorizationPolicies.StockWrite, AuthorizationPolicies.ReservationsWrite);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ReservingThenFulfillingMovesStockOutOfTheBin()
    {
        await _client.SeedStockAsync(onHand: 40);

        var reservation = await ReserveAsync(quantity: 15);
        Assert.Equal(25, (await LevelAsync()).Available);

        using var fulfilled = await _client.PostOkAsync($"api/reservations/{reservation.Id}/fulfilment", new { });

        Assert.Equal("Fulfilled", (await fulfilled.ReadAsync<ReservationResponse>()).Status);
        var level = await LevelAsync();
        Assert.Equal((25, 0, 25), (level.OnHand, level.Reserved, level.Available));
    }

    [Fact]
    public async Task ReleasingMakesTheStockAvailableAgain()
    {
        await _client.SeedStockAsync(onHand: 40);
        var reservation = await ReserveAsync(quantity: 40);

        using var released = await _client.PostOkAsync($"api/reservations/{reservation.Id}/release", new { });

        Assert.Equal(40, (await LevelAsync()).Available);
    }

    [Fact]
    public async Task ReservingMoreThanIsAvailableIsAConflict()
    {
        await _client.SeedStockAsync(onHand: 10);

        var status = await _client.StatusOfPostAsync("api/reservations", Reservation(quantity: 11));

        Assert.Equal(HttpStatusCode.Conflict, status);
    }

    [Fact]
    public async Task AReservationCannotBeFulfilledAfterItsHoldLapses()
    {
        await _client.SeedStockAsync(onHand: 10);
        var reservation = await ReserveAsync(quantity: 5, holdMinutes: 5);

        _factory.Clock.Advance(TimeSpan.FromMinutes(6));
        var status = await _client.StatusOfPostAsync($"api/reservations/{reservation.Id}/fulfilment", new { });

        Assert.Equal(HttpStatusCode.Conflict, status);
        Assert.Equal(10, (await LevelAsync()).OnHand);
    }

    [Fact]
    public async Task AReservationCanBeReadBack()
    {
        await _client.SeedStockAsync(onHand: 10);
        var reservation = await ReserveAsync(quantity: 3, holdMinutes: 30);

        var fetched = await _client.GetFromJsonAsync<ReservationResponse>($"api/reservations/{reservation.Id}", Token);

        Assert.Equal(reservation, fetched);
        Assert.Equal(_factory.Clock.GetUtcNow().AddMinutes(30), reservation.ExpiresAt);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static object Reservation(int quantity, int? holdMinutes = null) => new
    {
        skuCode = ApiClientExtensions.SkuCode,
        binCode = ApiClientExtensions.BinCode,
        quantity,
        holdMinutes,
    };

    private async Task<ReservationResponse> ReserveAsync(int quantity, int? holdMinutes = null)
    {
        using var response = await _client.PostOkAsync("api/reservations", Reservation(quantity, holdMinutes));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadAsync<ReservationResponse>();
    }

    private async Task<StockLevelResponse> LevelAsync()
    {
        var levels = await _client.GetFromJsonAsync<List<StockLevelResponse>>(StockPath, Token);
        return Assert.Single(levels ?? []);
    }
}
