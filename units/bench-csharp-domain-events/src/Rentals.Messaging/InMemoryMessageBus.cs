using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Rentals.Messaging;

/// <summary>
/// An in-process bus: published events are delivered to every registered <see cref="IIntegrationEventHandler{TEvent}"/>
/// in a fresh scope; sent commands are queued per destination for an out-of-process endpoint to collect.
/// </summary>
public sealed partial class InMemoryMessageBus(IServiceScopeFactory scopes, ILogger<InMemoryMessageBus> logger) : IMessageBus
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<object>> _endpoints = new(StringComparer.Ordinal);

    public async Task PublishAsync(IIntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        ArgumentNullException.ThrowIfNull(context);
        var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(integrationEvent.GetType());
        var handle = handlerType.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync))
            ?? throw new InvalidOperationException($"{handlerType.Name} declares no HandleAsync method.");
        var scope = scopes.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            foreach (var handler in scope.ServiceProvider.GetServices(handlerType).OfType<object>())
            {
                LogDelivering(logger, context.MessageType, context.MessageId, handler.GetType().Name);
                if (handle.Invoke(handler, [integrationEvent, context, cancellationToken]) is Task delivery)
                {
                    await delivery.ConfigureAwait(false);
                }
            }
        }
    }

    public Task SendAsync(object command, string destination, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        _endpoints.GetOrAdd(destination, _ => new ConcurrentQueue<object>()).Enqueue(command);
        LogSent(logger, command.GetType().Name, destination);
        return Task.CompletedTask;
    }

    /// <summary>Messages sent to <paramref name="destination"/> and not yet collected.</summary>
    public IReadOnlyList<object> Pending(string destination) =>
        _endpoints.TryGetValue(destination, out var queue) ? [.. queue] : [];

    [LoggerMessage(Level = LogLevel.Debug, Message = "Delivering {MessageType} {MessageId} to {Handler}")]
    private static partial void LogDelivering(ILogger logger, string messageType, Guid messageId, string handler);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Sent {CommandType} to {Destination}")]
    private static partial void LogSent(ILogger logger, string commandType, string destination);
}
