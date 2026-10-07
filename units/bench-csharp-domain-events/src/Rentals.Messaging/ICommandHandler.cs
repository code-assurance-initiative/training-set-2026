namespace Rentals.Messaging;

/// <summary>Executes one command type.</summary>
public interface ICommandHandler<in TCommand>
    where TCommand : notnull
{
    Task HandleAsync(TCommand command, CancellationToken cancellationToken);
}
