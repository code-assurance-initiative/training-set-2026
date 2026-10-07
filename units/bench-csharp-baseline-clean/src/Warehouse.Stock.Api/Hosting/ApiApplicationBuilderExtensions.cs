using Warehouse.Stock.Api.Security;

namespace Warehouse.Stock.Api.Hosting;

public static class ApiApplicationBuilderExtensions
{
    public static WebApplication UseStockApi(this WebApplication app)
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
