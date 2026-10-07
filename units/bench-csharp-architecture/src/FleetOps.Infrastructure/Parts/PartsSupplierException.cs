namespace FleetOps.Infrastructure.Parts;

public sealed class PartsSupplierException : Exception
{
    public PartsSupplierException()
    {
    }

    public PartsSupplierException(string message)
        : base(message)
    {
    }

    public PartsSupplierException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
