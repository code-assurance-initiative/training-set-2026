using Fx.Conversion.Api.Security;

namespace Fx.Conversion.Api.Hosting;

public static class ApiApplicationBuilderExtensions
{
    public static WebApplication UseConversionApi(this WebApplication app)
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
