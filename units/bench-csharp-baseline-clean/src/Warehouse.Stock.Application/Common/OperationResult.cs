using System.Diagnostics.CodeAnalysis;

namespace Warehouse.Stock.Application.Common;

public static class OperationResult
{
    public static OperationResult<T> Success<T>(T value) => new OperationResult<T>.Succeeded(value);
}

/// <summary>
/// The outcome of an application operation: a value, or an expected failure. Unexpected failures are exceptions.
/// </summary>
public abstract record OperationResult<T>
{
    private OperationResult()
    {
    }

    public static implicit operator OperationResult<T>(OperationError error) => new Failed(error);

    public abstract TOut Match<TOut>(Func<T, TOut> onSuccess, Func<OperationError, TOut> onFailure);

    public abstract bool TryGetValue(
        [MaybeNullWhen(false)] out T value,
        [NotNullWhen(false)] out OperationError? failure);

    public sealed record Succeeded(T Value) : OperationResult<T>
    {
        public override TOut Match<TOut>(Func<T, TOut> onSuccess, Func<OperationError, TOut> onFailure) =>
            onSuccess(Value);

        public override bool TryGetValue(
            [MaybeNullWhen(false)] out T value,
            [NotNullWhen(false)] out OperationError? failure)
        {
            value = Value;
            failure = null;
            return true;
        }
    }

    public sealed record Failed(OperationError Error) : OperationResult<T>
    {
        public override TOut Match<TOut>(Func<T, TOut> onSuccess, Func<OperationError, TOut> onFailure) =>
            onFailure(Error);

        public override bool TryGetValue(
            [MaybeNullWhen(false)] out T value,
            [NotNullWhen(false)] out OperationError? failure)
        {
            value = default;
            failure = Error;
            return false;
        }
    }
}
