using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Depot.Slots.Infrastructure.Postgres;

/// <summary>
/// Creates the bookings table on start-up if it does not exist. The statement is idempotent, so every replica
/// may run it; it is the only schema this service owns.
/// </summary>
public sealed partial class SchemaInitializer(NpgsqlDataSource dataSource, ILogger<SchemaInitializer> logger) : IHostedService
{
    private const string Schema =
        """
        CREATE EXTENSION IF NOT EXISTS btree_gist;
        CREATE TABLE IF NOT EXISTS bookings (
            id                uuid PRIMARY KEY,
            dock_code         text        NOT NULL,
            carrier_reference text        NOT NULL,
            starts_at         timestamptz NOT NULL,
            ends_at           timestamptz NOT NULL CHECK (ends_at > starts_at),
            reminder_sent_at  timestamptz NULL,
            EXCLUDE USING gist (dock_code WITH =, tstzrange(starts_at, ends_at) WITH &&)
        );
        """;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var command = dataSource.CreateCommand(Schema);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        LogSchemaReady();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Information, Message = "Bookings schema is in place")]
    private partial void LogSchemaReady();
}
