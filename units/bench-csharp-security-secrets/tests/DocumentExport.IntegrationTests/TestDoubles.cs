using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Encodings.Web;
using DocumentExport.Api.Exports;
using DocumentExport.Api.Notifications;
using DocumentExport.Api.Partner;
using DocumentExport.Api.Persistence;
using DocumentExport.Api.Storage;
using DocumentExport.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocumentExport.IntegrationTests;

internal sealed class InMemoryObjectStore : IObjectStore
{
    private readonly ConcurrentDictionary<string, byte[]> _objects = new();

    public Task UploadAsync(string objectKey, ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
    {
        _objects[objectKey] = content.ToArray();
        return Task.CompletedTask;
    }

    public Task<byte[]> DownloadAsync(string objectKey, CancellationToken cancellationToken) =>
        Task.FromResult(_objects[objectKey]);
}

internal sealed class InMemoryExportStore : IExportStore
{
    private readonly ConcurrentDictionary<Guid, ExportJob> _jobs = new();

    public static IReadOnlyList<InventoryRow> Inventory { get; } =
    [
        new("A-100", "Pallet wrap", 12, "R01-S2"),
        new("B-200", "Box, large", 0, "R02-S1"),
    ];

    public Task<IReadOnlyList<InventoryRow>> ReadInventoryAsync(string warehouseCode, bool includeZeroStock, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<InventoryRow>>(Inventory.Where(r => includeZeroStock || r.Quantity > 0).ToList());

    public Task InsertAsync(ExportJob job, CancellationToken cancellationToken)
    {
        _jobs[job.Id] = job;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Guid exportId, ExportStatus status, string? manifestSha256, CancellationToken cancellationToken)
    {
        _jobs[exportId] = _jobs[exportId] with { Status = status, ManifestSha256 = manifestSha256 };
        return Task.CompletedTask;
    }

    public Task<ExportJob?> FindAsync(Guid exportId, CancellationToken cancellationToken) =>
        Task.FromResult(_jobs.TryGetValue(exportId, out var job) ? job : null);
}

internal sealed class RecordingAuditLog : IAuditLog
{
    public ConcurrentQueue<(string Action, Guid ExportId)> Entries { get; } = new();

    public Task RecordAsync(string action, Guid exportId, string clientApplication, CancellationToken cancellationToken)
    {
        Entries.Enqueue((action, exportId));
        return Task.CompletedTask;
    }
}

internal sealed class SilentPartner : IPartnerApi, IDeliveryNotifier
{
    public Task AnnounceExportAsync(ExportJob job, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task ExportReadyAsync(ExportJob job, CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>Authenticates callers from the <c>X-Test-Roles</c> header, standing in for the identity provider.</summary>
internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string RolesHeader = "X-Test-Roles";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(RolesHeader, out var roles))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = roles.ToString().Split(',').Select(role => new Claim("roles", role)).Append(new Claim("azp", "integration-tests"));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName, "azp", "roles"));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
