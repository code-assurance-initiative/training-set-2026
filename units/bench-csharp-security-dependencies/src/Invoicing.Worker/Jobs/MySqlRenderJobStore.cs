using System.Data.Common;
using System.Text.Json;
using MySql.Data.MySqlClient;

namespace Invoicing.Worker.Jobs;

/// <summary>
/// The render queue is a table in the billing database. A job is claimed by stamping it with this worker's
/// instance id in one UPDATE, so two workers never take the same job.
/// </summary>
public sealed partial class MySqlRenderJobStore(string connectionString, string workerId, ILogger<MySqlRenderJobStore> logger) : IRenderJobStore
{
    private const int MaxReasonLength = 500;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<RenderJob>> ClaimAsync(int batchSize, CancellationToken cancellationToken)
    {
        var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var connectionScope = connection.ConfigureAwait(false);

        using (var claim = connection.CreateCommand())
        {
            claim.CommandText = """
                UPDATE render_jobs
                   SET status = 'claimed', claimed_by = @worker, claimed_at = UTC_TIMESTAMP(6)
                 WHERE status = 'queued'
                 ORDER BY id
                 LIMIT @batch
                """;
            claim.Parameters.AddWithValue("@worker", workerId);
            claim.Parameters.AddWithValue("@batch", batchSize);
            await claim.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        using var select = connection.CreateCommand();
        select.CommandText = "SELECT id, recipient, payload FROM render_jobs WHERE status = 'claimed' AND claimed_by = @worker ORDER BY id";
        select.Parameters.AddWithValue("@worker", workerId);

        var jobs = new List<RenderJob>();
        var reader = await select.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        await using var readerScope = reader.ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            jobs.Add(Read(reader));
        }

        LogClaimed(jobs.Count, workerId);
        return jobs;
    }

    public Task MarkSentAsync(long jobId, CancellationToken cancellationToken) =>
        SetStatusAsync(jobId, "sent", reason: null, cancellationToken);

    public Task MarkFailedAsync(long jobId, string reason, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reason);
        return SetStatusAsync(jobId, "failed", reason.Length > MaxReasonLength ? reason[..MaxReasonLength] : reason, cancellationToken);
    }

    private async Task SetStatusAsync(long jobId, string status, string? reason, CancellationToken cancellationToken)
    {
        var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var connectionScope = connection.ConfigureAwait(false);
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE render_jobs SET status = @status, failure_reason = @reason, finished_at = UTC_TIMESTAMP(6) WHERE id = @id AND claimed_by = @worker";
        command.Parameters.AddWithValue("@status", status);
        command.Parameters.AddWithValue("@reason", reason);
        command.Parameters.AddWithValue("@id", jobId);
        command.Parameters.AddWithValue("@worker", workerId);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<MySqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    private static RenderJob Read(DbDataReader reader)
    {
        var payload = JsonSerializer.Deserialize<InvoicePayload>(reader.GetString(2), Json)
            ?? throw new InvalidDataException($"Render job {reader.GetInt64(0)} has an empty payload.");
        return new RenderJob(reader.GetInt64(0), reader.GetString(1), payload.ToDocument());
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Claimed {Count} render job(s) as {WorkerId}")]
    private partial void LogClaimed(int count, string workerId);
}
