using Dapper;
using Npgsql;

namespace ReportDesk.Api.Documents;

public sealed class DocumentRepository(NpgsqlDataSource dataSource) : IDocumentRepository
{
    private const string Columns =
        "id, number, title, owner, body, metadata_xml AS MetadataXml, updated_at AS UpdatedAt";

    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["number"] = "number",
        ["title"] = "title",
        ["owner"] = "owner",
        ["updated"] = "updated_at",
    };

    public async Task<DocumentRecord?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false))
        {
            var command = new CommandDefinition(
                $"SELECT {Columns} FROM documents WHERE id = @Id",
                new { Id = id },
                cancellationToken: cancellationToken);
            return await connection.QuerySingleOrDefaultAsync<DocumentRecord>(command).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<DocumentRecord>> ListAsync(
        string? sort, bool descending, int limit, CancellationToken cancellationToken)
    {
        var column = ResolveSortColumn(sort);
        var direction = descending ? "DESC" : "ASC";
        var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false))
        {
            var sql = $"SELECT {Columns} FROM documents ORDER BY {column} {direction} LIMIT @Limit";
            var rows = await connection
                .QueryAsync<DocumentRecord>(new CommandDefinition(sql, new { Limit = limit }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);
            return rows.AsList();
        }
    }

    /// <summary>Maps a sort key from the query string onto a column name; unknown keys sort by last update.</summary>
    internal static string ResolveSortColumn(string? sort) =>
        sort is not null && SortColumns.TryGetValue(sort, out var column) ? column : "updated_at";
}
