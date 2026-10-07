using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rentals.Lending.Infrastructure.Persistence;
using Rentals.Messaging;

namespace Rentals.Lending.Infrastructure.Messaging;

/// <summary>
/// Publishes committed outbox messages, oldest first, and deletes each once the bus has accepted it. A crash between
/// the two publishes the message again on the next pass: delivery is at least once, and consumers de-duplicate by
/// <see cref="MessageContext.MessageId"/>, which is the outbox row's id.
/// </summary>
public sealed partial class OutboxDispatcher(
    IServiceScopeFactory scopes,
    IMessageBus bus,
    IOptions<OutboxOptions> options,
    TimeProvider clock,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    public async Task<int> DispatchPendingAsync(CancellationToken cancellationToken)
    {
        var scope = scopes.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var db = scope.ServiceProvider.GetRequiredService<LendingDbContext>();
            var pending = await db.OutboxMessages
                .Where(m => m.Attempts < options.Value.MaxAttempts)
                .OrderBy(m => m.OccurredAt)
                .Take(options.Value.BatchSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var published = 0;
            foreach (var message in pending)
            {
                try
                {
                    var context = new MessageContext(message.Id, message.Type, clock.GetUtcNow());
                    await bus.PublishAsync(IntegrationEventSerializer.FromOutbox(message), context, cancellationToken).ConfigureAwait(false);
                    db.OutboxMessages.Remove(message);
                    published++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogPublishFailed(logger, ex, message.Type, message.Id);
                    message.RecordFailure(ex.Message);
                }

                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            return published;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.PollInterval, clock);
        do
        {
            var published = await DispatchPendingAsync(stoppingToken).ConfigureAwait(false);
            if (published > 0)
            {
                LogPublished(logger, published);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Published {Count} outbox message(s)")]
    private static partial void LogPublished(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Publishing {MessageType} {MessageId} failed; it stays in the outbox")]
    private static partial void LogPublishFailed(ILogger logger, Exception exception, string messageType, Guid messageId);
}
