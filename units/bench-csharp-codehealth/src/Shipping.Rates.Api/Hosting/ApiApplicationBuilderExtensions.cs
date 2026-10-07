using Shipping.Rates.Api.Endpoints;
using Shipping.Rates.Api.Security;
using Shipping.Rates.Core.Pricing;

namespace Shipping.Rates.Api.Hosting;

public static class ApiApplicationBuilderExtensions
{
    public static WebApplication UseShippingApi(this WebApplication app)
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
        app.MapQuoteEndpoints();
        app.MapLabelEndpoints();
        app.MapWebhookEndpoints();

        app.Services.GetRequiredService<RateCardCache>().WarmUp();
        return app;
    }
}
