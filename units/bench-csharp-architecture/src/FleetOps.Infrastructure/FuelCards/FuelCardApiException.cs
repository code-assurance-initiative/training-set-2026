namespace FleetOps.Infrastructure.FuelCards;

public sealed class FuelCardApiException : Exception
{
    public FuelCardApiException()
    {
    }

    public FuelCardApiException(string message)
        : base(message)
    {
    }

    public FuelCardApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
