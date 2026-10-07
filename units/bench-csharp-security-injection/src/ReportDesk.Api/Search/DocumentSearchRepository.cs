using Dapper;
using Npgsql;

namespace ReportDesk.Api.Search;

/// <summary>Title search over the archive's <c>documents</c> table (trigram index on <c>title</c>).</summary>
public sealed class DocumentSearchRepository(NpgsqlDataSource dataSource) : IDocumentSearchRepository
{
    public async Task<IReadOnlyList<DocumentHit>> SearchAsync(string term, int limit, CancellationToken cancellationToken)
    {
        var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false))
        {
            var sql = $"""
                SELECT id, title, owner, classification, updated_at AS UpdatedAt
                FROM documents
                WHERE title ILIKE '%{term}%'
                ORDER BY updated_at DESC
                LIMIT {limit}
                """;
            var hits = await connection
                .QueryAsync<DocumentHit>(new CommandDefinition(sql, cancellationToken: cancellationToken))
                .ConfigureAwait(false);
            return hits.AsList();
        }
    }
}
