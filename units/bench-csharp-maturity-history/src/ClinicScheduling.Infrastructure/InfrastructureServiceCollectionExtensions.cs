using ClinicScheduling.Application.Abstractions;
using ClinicScheduling.Domain.Holidays;
using ClinicScheduling.Infrastructure.Holidays;
using ClinicScheduling.Infrastructure.Notifications;
using ClinicScheduling.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicScheduling.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddSchedulingInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IHolidayCalendar, TableHolidayCalendar>();
        services.AddSingleton<IAppointmentRepository, InMemoryAppointmentRepository>();
        services.AddSingleton<IScheduleRepository, InMemoryScheduleRepository>();
        services.AddSingleton<IPatientStrikeLedger, InMemoryStrikeLedger>();
        services.AddSingleton<IReminderOutbox, InMemoryReminderOutbox>();
        services.AddSingleton<ISmsSender, LoggingSmsSender>();
        services.AddOptions<ReminderDispatchOptions>().BindConfiguration(ReminderDispatchOptions.SectionName);
        services.AddHostedService<ReminderDispatchWorker>();
        return services;
    }
}
