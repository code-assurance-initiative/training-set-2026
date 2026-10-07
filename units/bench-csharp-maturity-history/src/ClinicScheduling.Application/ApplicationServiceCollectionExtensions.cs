using ClinicScheduling.Application.Availability;
using ClinicScheduling.Application.Booking;
using ClinicScheduling.Application.Reminders;
using ClinicScheduling.Application.Series;
using ClinicScheduling.Domain.Availability;
using ClinicScheduling.Domain.Policies;
using ClinicScheduling.Domain.Recurrence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ClinicScheduling.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddSchedulingApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<ReminderOptions>().BindConfiguration(ReminderOptions.SectionName);
        services.AddSingleton(new SlotSearchOptions());
        services.AddSingleton(new CancellationPolicyOptions());
        services.AddSingleton<SlotFinder>();
        services.AddSingleton<CancellationPolicy>();
        services.AddSingleton<RecurrenceExpander>();
        services.AddSingleton<ReminderPlanner>();
        services.AddScoped<BookAppointmentHandler>();
        services.AddScoped<CancelAppointmentHandler>();
        services.AddScoped<RescheduleAppointmentHandler>();
        services.AddScoped<RecordNoShowHandler>();
        services.AddScoped<FindSlotsHandler>();
        services.AddScoped<BookTreatmentSeriesHandler>();
        return services;
    }
}
