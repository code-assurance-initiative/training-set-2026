using Depot.Slots.Infrastructure.Postgres;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Depot.Slots.IntegrationTests.Postgres;

/// <summary>
/// A throwaway PostgreSQL server in a container, with the service's schema applied. Where no container runtime is
/// reachable the fixture records why, and every test that needs it is skipped rather than failed.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public NpgsqlDataSource? DataSource { get; private set; }

    public string? UnavailableReason { get; private set; }

    public async ValueTask InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
            await _container.StartAsync();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            UnavailableReason = $"No container runtime for PostgreSQL: {ex.GetType().Name}";
            return;
        }

        DataSource = NpgsqlDataSource.Create(_container.GetConnectionString());
        await new SchemaInitializer(DataSource, NullLogger<SchemaInitializer>.Instance).StartAsync(CancellationToken.None);
    }

    public async ValueTask DisposeAsync()
    {
        if (DataSource is not null)
        {
            await DataSource.DisposeAsync();
        }

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
