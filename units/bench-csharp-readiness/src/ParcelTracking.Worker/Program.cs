using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ParcelTracking.Infrastructure;
using ParcelTracking.Infrastructure.Persistence;
using ParcelTracking.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddTrackingPersistence(builder.Configuration);
builder.Services.AddCarrierApi();
builder.Services.AddMerchantWebhooks();
builder.Services.AddOptions<PollingOptions>()
    .BindConfiguration(PollingOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IHealthCheckPublisher, HeartbeatPublisher>();
builder.Services.Configure<HealthCheckPublisherOptions>(options => options.Period = TimeSpan.FromSeconds(30));
builder.Services.AddHostedService<TrackingPoller>();
builder.Services.AddHostedService<NotificationDispatcher>();
builder.Services.AddOptions<RetentionOptions>()
    .BindConfiguration(RetentionOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddHostedService<RetentionSweeper>();

builder.Logging.AddOpenTelemetry(logging => logging.IncludeScopes = true);
var telemetry = builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("parcel-tracking-worker"))
    .WithTracing(tracing => tracing.AddHttpClientInstrumentation())
    .WithMetrics(metrics => metrics.AddHttpClientInstrumentation());
if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
{
    telemetry.UseOtlpExporter();
}

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TrackingDbContext>();
    db.Database.EnsureCreated();
}

await host.RunAsync();
