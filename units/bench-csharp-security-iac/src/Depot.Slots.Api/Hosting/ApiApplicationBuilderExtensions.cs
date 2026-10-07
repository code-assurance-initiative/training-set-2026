using Depot.Slots.Api.Endpoints;
using Depot.Slots.Api.Security;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Depot.Slots.Api.Hosting;

public static class ApiApplicationBuilderExtensions
{
    public static WebApplication UseSlotsApi(this WebApplication app)
    {
        app.UseForwardedHeaders();
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        // The kubelet probes the pod directly over the pod network; everything else arrives through the ingress.
        app.UseWhen(context => !context.Request.Path.StartsWithSegments("/health"), branch => branch.UseHttpsRedirection());
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
        app.MapHealthChecks("/health/ready").AllowAnonymous();
        app.MapBookingEndpoints();
        return app;
    }
}
