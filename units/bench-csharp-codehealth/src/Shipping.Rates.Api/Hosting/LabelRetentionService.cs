using Microsoft.Extensions.Options;
using Shipping.Rates.Core.Labels;

namespace Shipping.Rates.Api.Hosting;

/// <summary>Deletes labels past their retention period (<see cref="LabelOptions.RetentionDays"/>) once an hour.</summary>
public sealed partial class LabelRetentionService(
    IServiceScopeFactory scopes,
    IOptions<LabelOptions> options,
    TimeProvider clock,
    ILogger<LabelRetentionService> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, clock);
        do
        {
            PurgeOnce();
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public int PurgeOnce()
    {
        using var scope = scopes.CreateScope();
        var purged = scope.ServiceProvider.GetRequiredService<LabelService>().PurgeExpired();
        if (purged > 0)
        {
            LogPurged(purged, options.Value.RetentionDays);
        }

        return purged;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted {Count} label(s) older than {Days} days")]
    private partial void LogPurged(int count, int days);
}
