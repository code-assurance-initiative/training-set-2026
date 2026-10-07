using System.Diagnostics.Metrics;
using FleetOps.Application.Abstractions;
using FleetOps.Application.Features.Vehicles;
using FleetOps.Application.Telemetry;
using FleetOps.Application.Tests.Support;
using FleetOps.Contracts.Paging;
using FleetOps.Contracts.Vehicles;
using FleetOps.Domain.Common;
using FleetOps.Infrastructure.Persistence;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace FleetOps.Application.Tests.Features;

public sealed class VehicleHandlerTests : IDisposable
{
    private readonly TestDatabase _database = new();

    [Fact]
    public async Task RegistersAVehicleAndRefusesASecondOneWithTheSameVin()
    {
        await using var db = _database.CreateContext();
        var handler = new RegisterVehicleHandler(new VehicleRepository(db), db);
        var command = new RegisterVehicleCommand("WVWZZZ1KZAW000001", "AB12345", "Transit", 42_000);

        var summary = await handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.Equal("AB12345", summary.Registration);
        Assert.Single(db.Vehicles);
        Assert.Single(db.AuditEntries);
        await Assert.ThrowsAsync<DomainException>(async () => await handler.Handle(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AVehicleReadIsCached()
    {
        var readModel = new CountingReadModel();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var meters = new TestMeterFactory();
        var handler = new GetVehicleHandler(readModel, cache, new FleetMetrics(meters), NullLogger<GetVehicleHandler>.Instance);
        var query = new GetVehicleQuery(readModel.Vehicle.Id);

        var first = await handler.Handle(query, TestContext.Current.CancellationToken);
        var second = await handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.Equal(readModel.Vehicle, first);
        Assert.Equal(first, second);
        Assert.Equal(1, readModel.Reads);
    }

    public void Dispose() => _database.Dispose();

    private sealed class CountingReadModel : IVehicleReadModel
    {
        public VehicleSummary Vehicle { get; } = new(Guid.NewGuid(), "WVWZZZ1KZAW000001", "AB12345", "Transit", 1, 1, "Active");

        public int Reads { get; private set; }

        public Task<VehicleSummary?> GetVehicleAsync(Guid vehicleId, CancellationToken cancellationToken)
        {
            Reads++;
            return Task.FromResult<VehicleSummary?>(vehicleId == Vehicle.Id ? Vehicle : null);
        }

        public Task<PagedResult<VehicleSummary>> ListVehiclesAsync(int page, int pageSize, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<int> CountActiveVehiclesAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<int> CountVehiclesInWorkshopAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<VehicleSummary>> FindByRegistrationAsync(string registrationPrefix, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<VehicleSummary>> ListOverdueForServiceAsync(int toleranceKm, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class TestMeterFactory : IMeterFactory
    {
        private readonly List<Meter> _meters = [];

        public Meter Create(MeterOptions options)
        {
            var meter = new Meter(options);
            _meters.Add(meter);
            return meter;
        }

        public void Dispose() => _meters.ForEach(m => m.Dispose());
    }
}
