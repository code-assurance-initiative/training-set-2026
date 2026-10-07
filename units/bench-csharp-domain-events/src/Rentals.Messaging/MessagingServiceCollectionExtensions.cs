using Microsoft.Extensions.DependencyInjection;

namespace Rentals.Messaging;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddInProcessMessaging(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryMessageBus>();
        services.AddSingleton<IMessageBus>(sp => sp.GetRequiredService<InMemoryMessageBus>());
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        return services;
    }
}
