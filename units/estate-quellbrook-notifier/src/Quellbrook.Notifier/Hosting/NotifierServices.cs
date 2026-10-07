using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Quellbrook.Notifier.Channels;
using Quellbrook.Notifier.Messaging;
using Quellbrook.Notifier.Notifications;
using Quellbrook.Notifier.Persistence;
using Quellbrook.Notifier.Retention;
using RabbitMQ.Client;

namespace Quellbrook.Notifier.Hosting;

public static class NotifierServices
{
    public static HostApplicationBuilder AddNotifier(this HostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var services = builder.Services;
        services.AddSingleton(TimeProvider.System);
        services.AddDbContext<NotifierDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Notifier")));

        services.AddOptions<RabbitMqOptions>().BindConfiguration(RabbitMqOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.AddSingleton<IConnectionFactory>(provider =>
            RabbitMqConnectionProvider.CreateFactory(provider.GetRequiredService<IOptions<RabbitMqOptions>>()));
        services.AddSingleton<IRabbitMqConnectionProvider, RabbitMqConnectionProvider>();
        services.AddSingleton<InboxProcessor>();
        services.AddHostedService<EventsConsumer>();

        services.AddOptions<EmailProviderOptions>().BindConfiguration(EmailProviderOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.AddHttpClient<IEmailSender, EmailSender>((provider, http) =>
                http.BaseAddress = provider.GetRequiredService<IOptions<EmailProviderOptions>>().Value.BaseAddress)
            .AddStandardResilienceHandler();
        services.AddOptions<SmsProviderOptions>().BindConfiguration(SmsProviderOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.AddHttpClient<ISmsSender, SmsSender>((provider, http) =>
                http.BaseAddress = provider.GetRequiredService<IOptions<SmsProviderOptions>>().Value.BaseAddress)
            .AddStandardResilienceHandler();

        services.AddScoped<NotificationService>();
        services.AddScoped<OrderPlacedHandler>();
        services.AddScoped<DeliveryHandlers>();

        services.AddOptions<RetentionOptions>().BindConfiguration(RetentionOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.AddHostedService<RetentionSweeper>();

        services.AddHealthChecks().AddDbContextCheck<NotifierDbContext>("database");
        services.AddOptions<HeartbeatOptions>().BindConfiguration(HeartbeatOptions.SectionName);
        services.AddHostedService<HeartbeatPublisher>();

        builder.Logging.AddOpenTelemetry(logging => logging.IncludeScopes = true);
        var telemetry = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("quellbrook-notifier"))
            .WithTracing(tracing => tracing.AddHttpClientInstrumentation())
            .WithMetrics(metrics => metrics.AddHttpClientInstrumentation());
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            telemetry.UseOtlpExporter();
        }

        return builder;
    }
}
