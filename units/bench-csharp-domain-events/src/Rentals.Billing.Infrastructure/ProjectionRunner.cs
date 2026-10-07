using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rentals.Billing.Application.Projections;
using Rentals.Billing.Domain.EventSourcing;

namespace Rentals.Billing.Infrastructure;

/// <summary>Feeds the event log to the balance projection from its checkpoint, in log order.</summary>
public sealed partial class ProjectionRunner(
    IEventStore store,
    IAccountBalanceViewStore views,
    AccountBalanceProjection projection,
    TimeProvider clock,
    ILogger<ProjectionRunner> logger) : BackgroundService
{
    private const int PageSize = 256;

    public async Task<int> CatchUpAsync(CancellationToken cancellationToken)
    {
        var projected = 0;
        while (true)
        {
            var page = await store.ReadAllAsync(views.Checkpoint, PageSize, cancellationToken).ConfigureAwait(false);
            foreach (var stored in page)
            {
                projection.Project(stored);
            }

            projected += page.Count;
            if (page.Count < PageSize)
            {
                return projected;
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1), clock);
        do
        {
            var projected = await CatchUpAsync(stoppingToken).ConfigureAwait(false);
            if (projected > 0)
            {
                LogProjected(logger, projected, views.Checkpoint);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Projected {Count} event(s); checkpoint {Checkpoint}")]
    private static partial void LogProjected(ILogger logger, int count, long checkpoint);
}
