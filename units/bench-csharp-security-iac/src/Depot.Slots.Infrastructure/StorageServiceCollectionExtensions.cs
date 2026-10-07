using Depot.Slots.Core.Bookings;
using Depot.Slots.Infrastructure.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Depot.Slots.Infrastructure;

public static class StorageServiceCollectionExtensions
{
    /// <summary>
    /// Registers the booking store named by <c>Storage:Provider</c>: <c>Postgres</c> (the default, using the
    /// <c>Slots</c> connection string) or <c>InMemory</c> for local development and tests.
    /// </summary>
    public static IServiceCollection AddBookingStorage(
        this IServiceCollection services, IConfiguration configuration, bool ownsSchema)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var provider = configuration["Storage:Provider"] ?? "Postgres";
        if (string.Equals(provider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            return services.AddSingleton<IBookingStore, InMemoryBookingStore>();
        }

        var connectionString = configuration.GetConnectionString("Slots")
            ?? throw new InvalidOperationException("ConnectionStrings:Slots is required when Storage:Provider is Postgres.");
        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
        services.AddSingleton<IBookingStore, PostgresBookingStore>();
        if (ownsSchema)
        {
            services.AddHostedService<SchemaInitializer>();
        }

        return services;
    }
}
