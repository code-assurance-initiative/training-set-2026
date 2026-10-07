namespace Warehouse.Stock.Application.Common;

public sealed record OperationError(ErrorKind Kind, string Message)
{
    public static OperationError NotFound(string message) => new(ErrorKind.NotFound, message);

    public static OperationError Conflict(string message) => new(ErrorKind.Conflict, message);

    public static OperationError Invalid(string message) => new(ErrorKind.Invalid, message);
}
