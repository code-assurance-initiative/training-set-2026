using Depot.Slots.Core.Bookings;
using Depot.Slots.Infrastructure;
using Depot.Slots.Infrastructure.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Depot.Slots.UnitTests.Hosting;

public sealed class StorageRegistrationTests
{
    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    [Fact]
    public void TheInMemoryProviderRegistersTheProcessLocalStore()
    {
        using var provider = new ServiceCollection()
            .AddBookingStorage(Config(("Storage:Provider", "inmemory")), ownsSchema: true)
            .BuildServiceProvider();

        Assert.IsType<InMemoryBookingStore>(provider.GetRequiredService<IBookingStore>());
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public void PostgresIsTheDefaultAndOnlyTheSchemaOwnerInitialisesIt(bool ownsSchema, int initializers)
    {
        var services = new ServiceCollection()
            .AddBookingStorage(Config(("ConnectionStrings:Slots", "Host=db.invalid;Database=slots")), ownsSchema);

        Assert.Contains(services, d => d.ImplementationType == typeof(PostgresBookingStore));
        Assert.Equal(initializers, services.Count(d => d.ServiceType == typeof(IHostedService)));
    }

    [Fact]
    public void PostgresWithoutAConnectionStringIsAConfigurationError()
    {
        var error = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddBookingStorage(Config(), ownsSchema: true));

        Assert.Contains("ConnectionStrings:Slots", error.Message, StringComparison.Ordinal);
    }
}
