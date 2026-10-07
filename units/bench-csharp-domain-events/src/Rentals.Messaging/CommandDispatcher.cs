using Microsoft.Extensions.DependencyInjection;

namespace Rentals.Messaging;

/// <summary>Resolves the registered handlers of a command from the container and runs them in registration order.</summary>
public sealed class CommandDispatcher(IServiceProvider services) : ICommandDispatcher
{
    public async Task DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken)
        where TCommand : notnull
    {
        ArgumentNullException.ThrowIfNull(command);
        var handlers = services.GetServices<ICommandHandler<TCommand>>().ToList();
        if (handlers.Count == 0)
        {
            throw new InvalidOperationException($"No handler is registered for {typeof(TCommand).Name}.");
        }

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);
        }
    }
}
