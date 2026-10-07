using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParcelTracking.Core.Carriers;

namespace ParcelTracking.Infrastructure.Carriers;

/// <summary>
/// The carriers' display names and tracking-page links, cached for an hour. A stale copy is served while a refresh
/// runs in the background, so a slow carrier API never delays a parcel lookup.
/// </summary>
public sealed partial class CarrierDirectory(IServiceScopeFactory scopeFactory, TimeProvider timeProvider, ILogger<CarrierDirectory> logger)
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromHours(1);

    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private volatile Snapshot? _snapshot;

    public async Task<CarrierInfo?> FindAsync(string carrierCode, CancellationToken cancellationToken)
    {
        var snapshot = _snapshot;
        if (snapshot is null)
        {
            await RefreshAsync(cancellationToken);
            snapshot = _snapshot;
        }
        else if (timeProvider.GetUtcNow() - snapshot.LoadedAt > MaxAge)
        {
            // Deliberately not the caller's token: this refresh outlives the request that noticed the stale copy (the
            // request's token is cancelled as soon as its response is written). The carrier client's total timeout
            // bounds it, and RefreshAsync logs its own failures.
            _ = RefreshAsync(CancellationToken.None);
        }

        return snapshot?.Carriers.GetValueOrDefault(carrierCode);
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (!await _refreshGate.WaitAsync(TimeSpan.Zero, cancellationToken))
        {
            return;
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var carrierClient = scope.ServiceProvider.GetRequiredService<ICarrierClient>();
            var carriers = await carrierClient.GetCarriersAsync(cancellationToken);
            _snapshot = new Snapshot(timeProvider.GetUtcNow(), carriers.ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase));
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutException or OperationCanceledException)
        {
            LogRefreshFailed(ex);
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Carrier directory refresh failed; serving the cached copy")]
    private partial void LogRefreshFailed(Exception exception);

    private sealed record Snapshot(DateTimeOffset LoadedAt, Dictionary<string, CarrierInfo> Carriers);
}
