using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DocumentExport.Api.Tokens;
using DocumentExport.Contracts;

namespace DocumentExport.Api.Exports;

/// <summary>HTTP surface of the export service.</summary>
public static class ExportEndpoints
{
    public static IEndpointRouteBuilder MapExportEndpoints(this IEndpointRouteBuilder app)
    {
        var exports = app.MapGroup("/exports").RequireAuthorization(ExportPolicies.Read);
        exports.MapPost("/", CreateAsync).RequireAuthorization(ExportPolicies.Write);
        exports.MapGet("/{exportId:guid}", GetAsync);
        exports.MapPost("/{exportId:guid}/download-token", IssueTokenAsync);

        app.MapGet("/downloads/{exportId:guid}", DownloadAsync).RequireAuthorization(ExportPolicies.Download);
        return app;
    }

    private static async Task<IResult> CreateAsync(
        ExportRequest request, ClaimsPrincipal user, ExportService service, CancellationToken cancellationToken)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), errors, validateAllProperties: true))
        {
            return TypedResults.ValidationProblem(errors
                .SelectMany(e => e.MemberNames.DefaultIfEmpty(string.Empty), (e, member) => (member, message: e.ErrorMessage ?? "Invalid."))
                .GroupBy(e => e.member, e => e.message)
                .ToDictionary(g => g.Key, g => g.ToArray()));
        }

        var response = await service.CreateAsync(request, ClientApplication(user), cancellationToken);
        return TypedResults.Created($"/exports/{response.ExportId:D}", response);
    }

    private static async Task<IResult> GetAsync(Guid exportId, ExportService service, CancellationToken cancellationToken)
    {
        var response = await service.FindAsync(exportId, cancellationToken);
        return response is null ? TypedResults.NotFound() : TypedResults.Ok(response);
    }

    private static async Task<IResult> IssueTokenAsync(
        Guid exportId, ExportService service, DownloadTokenService tokens, CancellationToken cancellationToken)
    {
        var response = await service.FindAsync(exportId, cancellationToken);
        if (response is null)
        {
            return TypedResults.NotFound();
        }

        return response.Status == ExportStatus.Ready
            ? TypedResults.Ok(tokens.Issue(exportId))
            : TypedResults.Conflict();
    }

    private static async Task<IResult> DownloadAsync(
        Guid exportId, ClaimsPrincipal user, ExportService service, HttpResponse httpResponse, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(user.FindFirstValue(DownloadTokenService.ExportIdClaim), out var granted) || granted != exportId)
        {
            return TypedResults.Forbid();
        }

        var download = await service.OpenAsync(exportId, cancellationToken);
        if (download is null)
        {
            return TypedResults.NotFound();
        }

        httpResponse.Headers["X-Content-SHA256"] = download.Manifest.Sha256;
        httpResponse.Headers["X-Manifest-Signature"] = download.Manifest.Signature;
        return TypedResults.File(download.Content, "text/csv", download.FileName);
    }

    private static string ClientApplication(ClaimsPrincipal user) =>
        user.FindFirstValue("azp") ?? user.FindFirstValue("appid") ?? "unknown";
}
