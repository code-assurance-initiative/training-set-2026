namespace DocumentExport.Api.Http;

/// <summary>Adds the response headers an API that serves no HTML should send.</summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var headers = context.Response.Headers;
        headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers.CacheControl = "no-store";
        return next(context);
    }
}
