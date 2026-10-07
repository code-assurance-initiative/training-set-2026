namespace Rentals.Messaging;

/// <summary>Routes a command to its handler inside the process.</summary>
public interface ICommandDispatcher
{
    Task DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken)
        where TCommand : notnull;
}
