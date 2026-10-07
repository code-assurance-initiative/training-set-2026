using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using ParcelTracking.Api.Labels;
using ParcelTracking.Api.Security;

namespace ParcelTracking.Api.Hosting;

public static class ApiPipeline
{
    public static WebApplication UseParcelTrackingApi(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseForwardedHeaders();
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler();
        }

        app.UseHttpsRedirection();
        app.UseStatusCodePages();

        app.Map("/labels", labels =>
        {
            labels.UseRouting();
            labels.UseAuthentication();
            labels.UseAuthorization();
            labels.UseEndpoints(endpoints => endpoints.MapLabelEndpoints());
        });

        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }).AllowAnonymous();
        app.MapGet("/.well-known/security.txt", () => Results.Text(SecurityTxt.Content, "text/plain; charset=utf-8")).AllowAnonymous();

        app.MapControllers();
        return app;
    }
}
