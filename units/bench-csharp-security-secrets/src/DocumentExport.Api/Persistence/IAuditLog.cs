namespace DocumentExport.Api.Persistence;

/// <summary>Append-only record of who exported what.</summary>
public interface IAuditLog
{
    Task RecordAsync(string action, Guid exportId, string clientApplication, CancellationToken cancellationToken);
}
