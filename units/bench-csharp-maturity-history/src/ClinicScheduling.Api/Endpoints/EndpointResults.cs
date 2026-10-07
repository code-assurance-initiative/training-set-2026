using ClinicScheduling.Application;

namespace ClinicScheduling.Api.Endpoints;

internal static class EndpointResults
{
    /// <summary>The problem response for a use case that did not succeed.</summary>
    public static IResult Problem<T>(OperationResult<T> result) => result.Status switch
    {
        OperationStatus.NotFound => Results.Problem(result.Error, statusCode: StatusCodes.Status404NotFound),
        OperationStatus.Conflict => Results.Problem(result.Error, statusCode: StatusCodes.Status409Conflict),
        OperationStatus.Invalid => Results.Problem(result.Error, statusCode: StatusCodes.Status422UnprocessableEntity),
        _ => throw new InvalidOperationException("A successful result has no problem."),
    };
}
