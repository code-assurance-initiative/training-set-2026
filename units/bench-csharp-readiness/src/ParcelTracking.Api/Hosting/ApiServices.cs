using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ParcelTracking.Api.Security;
using ParcelTracking.Infrastructure;

namespace ParcelTracking.Api.Hosting;

public static class ApiServices
{
    public static WebApplicationBuilder AddParcelTrackingApi(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var services = builder.Services;

        services.AddTrackingPersistence(builder.Configuration);
        services.AddCarrierApi();
        services.AddControllers();
        services.AddProblemDetails();
        services.AddHsts(options =>
        {
            options.MaxAge = TimeSpan.FromDays(365);
            options.IncludeSubDomains = true;
        });
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            var trustedNetwork = builder.Configuration["ForwardedHeaders:TrustedNetwork"];
            if (!string.IsNullOrEmpty(trustedNetwork))
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(trustedNetwork));
            }
        });

        services.AddOptions<JwtAuthenticationOptions>()
            .BindConfiguration(JwtAuthenticationOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(options => options.HasHttpsAuthority(), "Authentication:Authority must be an absolute https URL.")
            .ValidateOnStart();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.ConfigureOptions<ConfigureJwtBearerOptions>();
        services.AddAuthorizationBuilder().AddParcelTrackingPolicies();

        builder.Logging.AddOpenTelemetry(logging => logging.IncludeScopes = true);
        var telemetry = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("parcel-tracking-api"))
            .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation())
            .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation());
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            telemetry.UseOtlpExporter();
        }

        return builder;
    }
}
