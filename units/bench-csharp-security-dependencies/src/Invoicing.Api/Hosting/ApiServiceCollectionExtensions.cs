using Invoicing.Api.Branding;
using Invoicing.Api.Security;
using Invoicing.Api.Signing;
using Invoicing.Rendering;
using Invoicing.Rendering.Ubl;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Invoicing.Api.Hosting;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddInvoicingApi(this IServiceCollection services)
    {
        services.AddInvoiceRendering();
        services.AddControllers();
        services.AddProblemDetails();
        services.AddHealthChecks();
        services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(365));
        services.AddBranding();
        services.AddSigning();
        return services.AddInvoicingSecurity();
    }

    private static void AddBranding(this IServiceCollection services)
    {
        services
            .AddOptions<BrandingOptions>()
            .BindConfiguration(BrandingOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(options => options.BaseAddress is { Scheme: "https" }, "Branding:BaseAddress must be an https URL.")
            .ValidateOnStart();

        services.AddTransient<TransientRetryHandler>();
        services
            .AddHttpClient<ILogoSource, BrandingServiceClient>((provider, http) =>
            {
                var options = provider.GetRequiredService<IOptions<BrandingOptions>>().Value;
                http.BaseAddress = options.BaseAddress;
                http.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddHttpMessageHandler<TransientRetryHandler>();
    }

    private static void AddSigning(this IServiceCollection services)
    {
        services
            .AddOptions<SigningCertificateOptions>()
            .BindConfiguration(SigningCertificateOptions.SectionName)
            .ValidateDataAnnotations();
        services.AddSingleton<ISigningCertificateSource, FileSigningCertificateSource>();
    }

    private static IServiceCollection AddInvoicingSecurity(this IServiceCollection services)
    {
        services
            .AddOptions<JwtAuthenticationOptions>()
            .BindConfiguration(JwtAuthenticationOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(options => options.HasHttpsAuthority(), "Authentication:Authority must be an absolute https URL.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.ConfigureOptions<ConfigureJwtBearerOptions>();
        services.AddAuthorizationBuilder().AddInvoicingPolicies();
        return services;
    }
}
