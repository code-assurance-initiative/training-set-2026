using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Rentals.Billing.Application.Handlers;
using Rentals.Billing.Application.Payments;
using Rentals.Billing.Application.Projections;
using Rentals.Contracts.IntegrationEvents;
using Rentals.Lending.Application.Loans;
using Rentals.Messaging;

namespace Rentals.Billing.Application;

public static class BillingApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddBillingApplication(this IServiceCollection services)
    {
        services.AddOptions<BillingOptions>().BindConfiguration(BillingOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.AddHttpClient<LoanOpenedHandler>((sp, http) =>
        {
            http.BaseAddress = sp.GetRequiredService<IOptions<BillingOptions>>().Value.CatalogueBaseAddress;
            http.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddScoped<IIntegrationEventHandler<LoanOpenedIntegrationEvent>>(sp => sp.GetRequiredService<LoanOpenedHandler>());
        services.AddScoped<IIntegrationEventHandler<MemberRegisteredIntegrationEvent>, MemberRegisteredHandler>();
        services.AddScoped<IIntegrationEventHandler<LoanReturnedIntegrationEvent>, LoanReturnedHandler>();
        services.AddScoped<IIntegrationEventHandler<EquipmentDamageReportedIntegrationEvent>, EquipmentDamageReportedHandler>();
        services.AddScoped<IIntegrationEventHandler<MemberSuspendedIntegrationEvent>, MemberSuspendedHandler>();
        services.AddScoped<ICommandHandler<ExtendLoanCommand>, ChargeExtensionFeeHandler>();
        services.AddScoped<ICommandHandler<RecordPaymentCommand>, RecordPaymentHandler>();
        services.AddScoped<IQueryHandler<AccountBalanceQuery, AccountBalanceView?>, AccountBalanceQueryHandler>();
        services.AddSingleton<AccountBalanceProjection>();
        return services;
    }
}
