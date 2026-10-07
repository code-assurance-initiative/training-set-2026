using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Warehouse.Stock.Application.Reservations;

namespace Warehouse.Stock.Infrastructure.Workers;

/// <summary>Periodically expires reservations whose hold has lapsed, so that their stock becomes available again.</summary>
public sealed partial class ReservationExpiryWorker(
    ReservationService reservations,
    IOptions<ReservationOptions> options,
    TimeProvider clock,
    ILogger<ReservationExpiryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = options.Value.ExpirySweepInterval;
        LogStarted(interval);

        using var timer = new PeriodicTimer(interval, clock);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await reservations.ExpireDueAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Reservation expiry sweep runs every {Interval}")]
    private partial void LogStarted(TimeSpan interval);
}
