using Microsoft.Extensions.Options;
using Npgsql;

namespace DocumentExport.Api.Persistence;

/// <summary>Writes audit entries to the audit database.</summary>
public sealed class AuditLog : IAuditLog, IAsyncDisposable
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly AuditStoreSettings _settings;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AuditLog> _logger;

    public AuditLog(IOptions<AuditStoreSettings> settings, TimeProvider timeProvider, ILogger<AuditLog> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings.Value;
        _dataSource = NpgsqlDataSource.Create(_settings.ConnectionString);
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task RecordAsync(string action, Guid exportId, string clientApplication, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO audit_entries (occurred_at, action, export_id, client_application)
            VALUES (@occurred_at, @action, @export_id, @client)
            """;

        await using var command = _dataSource.CreateCommand(sql);
        command.CommandTimeout = _settings.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("occurred_at", _timeProvider.GetUtcNow());
        command.Parameters.AddWithValue("action", action);
        command.Parameters.AddWithValue("export_id", exportId);
        command.Parameters.AddWithValue("client", clientApplication);
        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("Audited {Action} of export {ExportId} by {ClientApplication}", action, exportId, clientApplication);
    }

    public ValueTask DisposeAsync() => _dataSource.DisposeAsync();
}
