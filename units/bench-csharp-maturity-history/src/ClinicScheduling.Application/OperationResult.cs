namespace ClinicScheduling.Application;

public enum OperationStatus
{
    Ok,
    NotFound,
    Conflict,
    Invalid,
}

/// <summary>The outcome of a use case: a value, or the reason there is none.</summary>
public sealed record OperationResult<T>(OperationStatus Status, T? Value, string? Error);

public static class Operation
{
    public static OperationResult<T> Ok<T>(T value) => new(OperationStatus.Ok, value, null);

    public static OperationResult<T> NotFound<T>(string error) => new(OperationStatus.NotFound, default, error);

    public static OperationResult<T> Conflict<T>(string error) => new(OperationStatus.Conflict, default, error);

    public static OperationResult<T> Invalid<T>(string error) => new(OperationStatus.Invalid, default, error);
}
