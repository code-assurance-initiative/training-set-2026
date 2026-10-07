namespace Rentals.Lending.Application;

/// <summary>The command or query names something that does not exist.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException()
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public static NotFoundException For<TId>(string what, TId id) => new($"{what} {id} does not exist.");
}
