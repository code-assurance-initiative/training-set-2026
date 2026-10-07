using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Quellbrook.Notifier.Channels;
using Quellbrook.Notifier.Hosting;
using Quellbrook.Notifier.Messaging;
using Quellbrook.Notifier.Notifications;
using Quellbrook.Notifier.Retention;

namespace Quellbrook.Notifier.UnitTests.Hosting;

public sealed class NotifierServicesTests
{
    private static HostApplicationBuilder Builder(Dictionary<string, string?> settings)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = "Testing" });
        builder.Configuration.AddInMemoryCollection(settings);
        return builder.AddNotifier();
    }

    private static Dictionary<string, string?> Complete() => new()
    {
        ["ConnectionStrings:Notifier"] = "Host=localhost;Database=notifier",
        ["RabbitMq:Uri"] = "amqps://rabbitmq.test:5671/quellbrook",
        ["RabbitMq:UserName"] = "notifier",
        ["RabbitMq:Password"] = "unit-test-broker-credential",
        ["EmailProvider:ApiKey"] = "unit-test-provider-key",
        ["EmailProvider:FromAddress"] = "deliveries@quellbrook.example",
        ["SmsProvider:ApiKey"] = "unit-test-gateway-key",
    };

    [Fact]
    public void EveryHandlerChannelAndBackgroundServiceIsRegistered()
    {
        using var host = Builder(Complete()).Build();
        using var scope = host.Services.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<OrderPlacedHandler>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<DeliveryHandlers>());
        Assert.IsType<EmailSender>(scope.ServiceProvider.GetRequiredService<IEmailSender>());
        Assert.IsType<SmsSender>(scope.ServiceProvider.GetRequiredService<ISmsSender>());
        var hosted = host.Services.GetServices<IHostedService>().Select(service => service.GetType()).ToList();
        Assert.Contains(typeof(EventsConsumer), hosted);
        Assert.Contains(typeof(RetentionSweeper), hosted);
        Assert.Contains(typeof(HeartbeatPublisher), hosted);
    }

    [Fact]
    public void AMissingProviderKeyStopsTheWorkerFromStarting()
    {
        var settings = Complete();
        settings["EmailProvider:ApiKey"] = "";
        using var host = Builder(settings).Build();

        var exception = Assert.Throws<OptionsValidationException>(() => host.Services.GetRequiredService<IOptions<EmailProviderOptions>>().Value);
        Assert.Contains("ApiKey", exception.Message, StringComparison.Ordinal);
    }
}
