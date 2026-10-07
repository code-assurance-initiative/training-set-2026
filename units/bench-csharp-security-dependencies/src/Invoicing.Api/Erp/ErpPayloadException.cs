namespace Invoicing.Api.Erp;

/// <summary>The ERP sent a payload that cannot be turned into an invoice; the message says which field.</summary>
public sealed class ErpPayloadException : Exception
{
    public ErpPayloadException()
    {
    }

    public ErpPayloadException(string message)
        : base(message)
    {
    }

    public ErpPayloadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
