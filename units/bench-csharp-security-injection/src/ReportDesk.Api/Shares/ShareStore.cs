using Dapper;
using Npgsql;

namespace ReportDesk.Api.Shares;

public sealed class ShareStore(NpgsqlDataSource dataSource) : IShareStore
{
    public async Task AddAsync(ShareLink link, CancellationToken cancellationToken)
    {
        var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false))
        {
            await connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO share_links (token, document_id, password_hash, created_by, expires_at) " +
                "VALUES (@Token, @DocumentId, @PasswordHash, @CreatedBy, @ExpiresAt)",
                link,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<ShareLink?> FindAsync(string token, CancellationToken cancellationToken)
    {
        var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false))
        {
            return await connection.QuerySingleOrDefaultAsync<ShareLink>(new CommandDefinition(
                "SELECT token, document_id AS DocumentId, password_hash AS PasswordHash, created_by AS CreatedBy, " +
                "expires_at AS ExpiresAt FROM share_links WHERE token = @token",
                new { token },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<int> PurgeExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false))
        {
            return await connection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM share_links WHERE expires_at < @now",
                new { now },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }
}
