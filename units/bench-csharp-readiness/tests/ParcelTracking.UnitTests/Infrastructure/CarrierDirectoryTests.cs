using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using ParcelTracking.Core.Carriers;
using ParcelTracking.Infrastructure.Carriers;
using ParcelTracking.UnitTests.TestSupport;

namespace ParcelTracking.UnitTests.Infrastructure;

public sealed class CarrierDirectoryTests
{
    private readonly FakeCarrierClient _carrier = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero));
    private readonly CarrierDirectory _directory;

    public CarrierDirectoryTests()
    {
        _carrier.Carriers.Add(new CarrierInfo("NORDPOST", "Nordpost", new Uri("https://nordpost.test/t/{0}")));
        var provider = new ServiceCollection().AddSingleton<ICarrierClient>(_carrier).BuildServiceProvider();
        _directory = new CarrierDirectory(provider.GetRequiredService<IServiceScopeFactory>(), _clock, NullLogger<CarrierDirectory>.Instance);
    }

    [Fact]
    public async Task The_first_lookup_loads_the_directory()
    {
        var carrier = await _directory.FindAsync("nordpost", TestContext.Current.CancellationToken);

        Assert.Equal("Nordpost", carrier?.DisplayName);
        Assert.Equal(1, _carrier.CarrierListCalls);
    }

    [Fact]
    public async Task A_fresh_directory_is_served_from_the_cache()
    {
        await _directory.FindAsync("NORDPOST", TestContext.Current.CancellationToken);
        _clock.Advance(TimeSpan.FromMinutes(30));
        await _directory.FindAsync("NORDPOST", TestContext.Current.CancellationToken);

        Assert.Equal(1, _carrier.CarrierListCalls);
    }

    [Fact]
    public async Task A_stale_directory_is_served_while_it_refreshes()
    {
        await _directory.FindAsync("NORDPOST", TestContext.Current.CancellationToken);
        _clock.Advance(TimeSpan.FromHours(2));

        var carrier = await _directory.FindAsync("NORDPOST", TestContext.Current.CancellationToken);

        Assert.Equal("Nordpost", carrier?.DisplayName);
        Assert.Equal(2, _carrier.CarrierListCalls);
    }
}
