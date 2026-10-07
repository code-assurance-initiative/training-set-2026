using HarbourLane.Bookings.Web.Components;
using HarbourLane.Bookings.Web.Security;

namespace HarbourLane.Bookings.Web.Hosting;

internal static class PortalApplicationExtensions
{
    public static WebApplication UseBookingPortal(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();
        app.UseMiddleware<SecurityHeadersMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapHealthChecks("/health").AllowAnonymous();
        app.MapAccountEndpoints();
        app.MapRazorPages();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode(options => options.ContentSecurityFrameAncestorsPolicy = null);
        return app;
    }
}
