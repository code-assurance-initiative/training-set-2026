using Invoicing.Api.Security;

namespace Invoicing.Api.Hosting;

public static class ApiApplicationBuilderExtensions
{
    public static WebApplication UseInvoicingApi(this WebApplication app)
    {
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
