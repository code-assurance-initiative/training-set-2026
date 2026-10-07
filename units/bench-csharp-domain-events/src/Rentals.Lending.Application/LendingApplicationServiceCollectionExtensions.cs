using Microsoft.Extensions.DependencyInjection;
using Rentals.Contracts.IntegrationEvents;
using Rentals.Lending.Application.Catalogue;
using Rentals.Lending.Application.Loans;
using Rentals.Lending.Application.Members;
using Rentals.Lending.Application.Reservations;
using Rentals.Lending.Domain.Catalogue;
using Rentals.Messaging;

namespace Rentals.Lending.Application;

public static class LendingApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddLendingApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<RegisterMemberCommand>, RegisterMemberHandler>();
        services.AddScoped<ICommandHandler<SuspendMemberCommand>, SuspendMemberHandler>();
        services.AddScoped<ICommandHandler<RenewMembershipCommand>, RenewMembershipHandler>();
        services.AddScoped<ICommandHandler<EraseMemberPersonalDataCommand>, EraseMemberPersonalDataHandler>();
        services.AddScoped<ICommandHandler<RegisterEquipmentCommand>, RegisterEquipmentHandler>();
        services.AddScoped<ICommandHandler<RecordMaintenanceCommand>, RecordMaintenanceHandler>();
        services.AddScoped<ICommandHandler<RelocateEquipmentCommand>, RelocateEquipmentHandler>();
        services.AddScoped<ICommandHandler<CheckoutEquipmentCommand>, CheckoutEquipmentHandler>();
        services.AddScoped<ICommandHandler<ExtendLoanCommand>, ExtendLoanHandler>();
        services.AddScoped<ICommandHandler<ReturnEquipmentCommand>, ReturnEquipmentHandler>();
        services.AddScoped<IQueryHandler<SearchMembersQuery, IReadOnlyList<MemberSummary>>, SearchMembersHandler>();
        services.AddScoped<IQueryHandler<OverdueLoansQuery, IReadOnlyList<OverdueLoan>>, OverdueLoansHandler>();
        services.AddScoped<IIntegrationEventHandler<EquipmentDamageReportedIntegrationEvent>, MarkUnitDamagedHandler>();
        services.AddScoped<UnitAvailabilityService>();
        services.AddScoped<ReservationService>();
        services.TryAddSingletonTimeProvider();
        return services;
    }

    private static void TryAddSingletonTimeProvider(this IServiceCollection services)
    {
        if (!services.Any(d => d.ServiceType == typeof(TimeProvider)))
        {
            services.AddSingleton(TimeProvider.System);
        }
    }
}
