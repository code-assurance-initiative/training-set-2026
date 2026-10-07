using ReportDesk.Api.Security;

namespace ReportDesk.Api.Hosting;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseReportDesk(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseHttpsRedirection();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHealthChecks("/health").AllowAnonymous();
        app.MapControllers();
        return app;
    }
}
