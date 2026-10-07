using Microsoft.AspNetCore.Mvc;
using Warehouse.Stock.Application.Common;

namespace Warehouse.Stock.Api.Errors;

/// <summary>Turns application results into HTTP responses; expected failures become RFC 9457 problem details.</summary>
public static class ProblemMapping
{
    public static ActionResult<TResponse> ToOk<T, TResponse>(
        this ControllerBase controller,
        OperationResult<T> result,
        Func<T, TResponse> map) =>
        result.Match<ActionResult<TResponse>>(
            value => controller.Ok(map(value)),
            error => controller.ToProblem(error));

    public static ActionResult<TResponse> ToCreated<T, TResponse>(
        this ControllerBase controller,
        OperationResult<T> result,
        Func<T, TResponse> map,
        string actionName,
        Func<T, object> routeValues) =>
        result.Match<ActionResult<TResponse>>(
            value => controller.CreatedAtAction(actionName, routeValues(value), map(value)),
            error => controller.ToProblem(error));

    public static ActionResult ToProblem(this ControllerBase controller, OperationError error) =>
        controller.Problem(detail: error.Message, statusCode: StatusCodeFor(error.Kind));

    private static int StatusCodeFor(ErrorKind kind) => kind switch
    {
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest,
    };
}
