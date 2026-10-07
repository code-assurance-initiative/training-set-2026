using Depot.Slots.Core.Bookings;
using Npgsql;

namespace Depot.Slots.Infrastructure.Postgres;

/// <summary>
/// Bookings in PostgreSQL. The no-overlap rule is the table's exclusion constraint (see
/// <see cref="SchemaInitializer"/>), so two replicas booking the same window race safely: one insert wins.
/// </summary>
public sealed class PostgresBookingStore(NpgsqlDataSource dataSource) : IBookingStore
{
    private const string ExclusionViolation = "23P01";

    public async Task<IReadOnlyList<Booking>> ListForDockAsync(
        string dockCode, DateTimeOffset windowStart, DateTimeOffset windowEnd, CancellationToken cancellationToken)
    {
        using var command = dataSource.CreateCommand(
            """
            SELECT id, dock_code, carrier_reference, starts_at, ends_at, reminder_sent_at
            FROM bookings
            WHERE dock_code = $1 AND starts_at < $3 AND $2 < ends_at
            ORDER BY starts_at
            """);
        command.Parameters.Add(new NpgsqlParameter { Value = dockCode });
        command.Parameters.Add(new NpgsqlParameter { Value = windowStart.ToUniversalTime() });
        command.Parameters.Add(new NpgsqlParameter { Value = windowEnd.ToUniversalTime() });
        return await ReadAllAsync(command, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> TryAddAsync(Booking booking, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(booking);
        using var command = dataSource.CreateCommand(
            """
            INSERT INTO bookings (id, dock_code, carrier_reference, starts_at, ends_at)
            VALUES ($1, $2, $3, $4, $5)
            """);
        command.Parameters.Add(new NpgsqlParameter { Value = booking.Id });
        command.Parameters.Add(new NpgsqlParameter { Value = booking.DockCode });
        command.Parameters.Add(new NpgsqlParameter { Value = booking.CarrierReference });
        command.Parameters.Add(new NpgsqlParameter { Value = booking.StartsAt.ToUniversalTime() });
        command.Parameters.Add(new NpgsqlParameter { Value = booking.EndsAt.ToUniversalTime() });
        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (PostgresException ex) when (ex.SqlState == ExclusionViolation)
        {
            return false;
        }
    }

    public async Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        using var command = dataSource.CreateCommand("DELETE FROM bookings WHERE id = $1");
        command.Parameters.Add(new NpgsqlParameter { Value = id });
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1;
    }

    public async Task<IReadOnlyList<Booking>> ListDueForReminderAsync(DateTimeOffset until, CancellationToken cancellationToken)
    {
        using var command = dataSource.CreateCommand(
            """
            SELECT id, dock_code, carrier_reference, starts_at, ends_at, reminder_sent_at
            FROM bookings
            WHERE reminder_sent_at IS NULL AND starts_at <= $1
            ORDER BY starts_at
            """);
        command.Parameters.Add(new NpgsqlParameter { Value = until.ToUniversalTime() });
        return await ReadAllAsync(command, cancellationToken).ConfigureAwait(false);
    }

    public async Task MarkRemindedAsync(Guid id, DateTimeOffset sentAt, CancellationToken cancellationToken)
    {
        using var command = dataSource.CreateCommand("UPDATE bookings SET reminder_sent_at = $2 WHERE id = $1");
        command.Parameters.Add(new NpgsqlParameter { Value = id });
        command.Parameters.Add(new NpgsqlParameter { Value = sentAt.ToUniversalTime() });
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<Booking>> ReadAllAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        var bookings = new List<Booking>();
        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        await using (reader.ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                bookings.Add(new Booking(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetFieldValue<DateTimeOffset>(3),
                    reader.GetFieldValue<DateTimeOffset>(4),
                    await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false)
                        ? null
                        : reader.GetFieldValue<DateTimeOffset>(5)));
            }
        }

        return bookings;
    }
}
