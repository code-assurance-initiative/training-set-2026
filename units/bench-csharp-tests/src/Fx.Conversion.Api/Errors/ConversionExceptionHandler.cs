using Fx.Conversion.Currencies;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Fx.Conversion.Api.Errors;

/// <summary>
/// Turns the domain's argument errors into 400 problem responses; everything else stays a 500 from the framework
/// handler, without details.
/// </summary>
public sealed partial class ConversionExceptionHandler(
    IProblemDetailsService problems,
    ILogger<ConversionExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        var (title, detail) = exception switch
        {
            UnknownCurrencyException unknown => ("Unknown currency", $"'{unknown.Code}' is not a supported currency."),
            ArgumentException argument => ("Invalid request", argument.Message),
            _ => (null, null),
        };
        if (title is null)
        {
            return false;
        }

        LogRejected(title, httpContext.Request.Path);
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = title, Detail = detail },
            Exception = exception,
        }).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Rejected request to {Path}: {Reason}")]
    private partial void LogRejected(string reason, PathString path);
}
