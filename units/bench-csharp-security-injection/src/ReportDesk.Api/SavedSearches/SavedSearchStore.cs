using Npgsql;
using NpgsqlTypes;

namespace ReportDesk.Api.SavedSearches;

/// <summary>Saved searches predate the Dapper/EF split (ADR 0001) and still use plain Npgsql commands.</summary>
public sealed class SavedSearchStore(NpgsqlDataSource dataSource) : ISavedSearchStore
{
    public async Task SaveAsync(string owner, SavedSearchDefinition search, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(search);
        var command = dataSource.CreateCommand();
        await using (command.ConfigureAwait(false))
        {
            command.CommandText =
                "INSERT INTO saved_searches (owner, name, term, parameters) VALUES (@owner, @name, @term, @parameters) " +
                "ON CONFLICT (owner, name) DO UPDATE SET term = excluded.term, parameters = excluded.parameters";
            command.Parameters.AddWithValue("owner", owner);
            command.Parameters.AddWithValue("name", search.Name);
            command.Parameters.AddWithValue("term", search.Term);
            command.Parameters.AddWithValue("parameters", NpgsqlDbType.Jsonb, SavedSearchSerializer.Export([search]));
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<int> DeleteAsync(string owner, string name, CancellationToken cancellationToken)
    {
        var command = dataSource.CreateCommand();
        await using (command.ConfigureAwait(false))
        {
            command.CommandText = "DELETE FROM saved_searches WHERE owner = '" + owner + "' AND name = '" + name + "'";
            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
