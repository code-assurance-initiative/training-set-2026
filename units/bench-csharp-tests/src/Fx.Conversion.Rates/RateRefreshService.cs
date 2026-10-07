using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fx.Conversion.Rates.Ecb;

/// <summary>
/// Keeps the <see cref="RateCache"/> warm: refreshes it at start-up and then every
/// <see cref="EcbOptions.RefreshInterval"/>. A failed refresh is logged and retried on the next tick; the cache keeps
/// serving the previous table until its time-to-live runs out.
/// </summary>
public sealed partial class RateRefreshService(
    RateCache cache,
    TimeProvider clock,
    IOptions<EcbOptions> options,
    ILogger<RateRefreshService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.RefreshInterval, clock);
        do
        {
            await RefreshOnceAsync(stoppingToken).ConfigureAwait(false);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    private async Task RefreshOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            await cache.RefreshAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            LogRefreshFailed(ex);
        }
        catch (FormatException ex)
        {
            LogRefreshFailed(ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Rate refresh failed; serving the cached table until it expires")]
    private partial void LogRefreshFailed(Exception exception);
}
