using Depot.Slots.Core.Reminders;
using Depot.Slots.Infrastructure;
using Depot.Slots.Reminders.Chat;

namespace Depot.Slots.Reminders;

public static class RemindersServiceCollectionExtensions
{
    public static IServiceCollection AddReminders(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddSingleton(TimeProvider.System);
        services.AddBookingStorage(configuration, ownsSchema: false);
        services
            .AddOptions<ReminderOptions>()
            .BindConfiguration(ReminderOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services
            .AddOptions<ChatOptions>()
            .BindConfiguration(ChatOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(options => options.HasHttpsEndpoint(), "Chat:Endpoint must be an absolute https URL.")
            .ValidateOnStart();

        services.AddHttpClient<IReminderSender, ChatReminderSender>().AddStandardResilienceHandler();
        services.AddSingleton<ReminderDispatcher>();
        services.AddHostedService<ReminderWorker>();
        services.AddHealthChecks().AddCheck<ReminderWorkerHealthCheck>("reminder-worker");
        return services;
    }
}
