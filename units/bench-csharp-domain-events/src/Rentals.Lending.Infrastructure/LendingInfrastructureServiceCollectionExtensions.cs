using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rentals.Lending.Application;
using Rentals.Lending.Domain.Catalogue;
using Rentals.Lending.Domain.Loans;
using Rentals.Lending.Domain.Maintenance;
using Rentals.Lending.Domain.Members;
using Rentals.Lending.Domain.Reservations;
using Rentals.Lending.Infrastructure.Messaging;
using Rentals.Lending.Infrastructure.Persistence;
using Rentals.Lending.Infrastructure.Persistence.Repositories;
using Rentals.SharedKernel;

namespace Rentals.Lending.Infrastructure;

public static class LendingInfrastructureServiceCollectionExtensions
{
    /// <summary>Registers the Lending context on SQLite, with its repositories and the outbox dispatcher's options.</summary>
    public static IServiceCollection AddLending(this IServiceCollection services, Action<DbContextOptionsBuilder> database)
    {
        services.AddLendingApplication();
        services.AddDbContext<LendingDbContext>(database);
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<LendingDbContext>());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<LendingDbContext>());
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<IEquipmentRepository, EquipmentRepository>();
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IMaintenanceRecordRepository, MaintenanceRecordRepository>();
        services.AddOptions<OutboxOptions>().BindConfiguration(OutboxOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.AddSingleton<OutboxDispatcher>();
        return services;
    }
}
