using Microsoft.Extensions.DependencyInjection;
using Rentals.Billing.Application;
using Rentals.Billing.Application.Projections;
using Rentals.Billing.Domain.Accounts;
using Rentals.Billing.Domain.EventSourcing;
using Rentals.Messaging;

namespace Rentals.Billing.Infrastructure;

public static class BillingInfrastructureServiceCollectionExtensions
{
    /// <summary>Registers the Billing context with the in-memory event store, inbox and balance view.</summary>
    public static IServiceCollection AddBilling(this IServiceCollection services)
    {
        services.AddBillingApplication();
        services.AddSingleton<InMemoryEventStore>();
        services.AddSingleton<IEventStore>(sp => sp.GetRequiredService<InMemoryEventStore>());
        services.AddSingleton<IInboxStore>(sp => sp.GetRequiredService<InMemoryEventStore>());
        services.AddScoped<IMemberAccountRepository, EventSourcedMemberAccountRepository>();
        services.AddSingleton<IAccountBalanceViewStore, InMemoryAccountBalanceViewStore>();
        services.AddSingleton<ProjectionRunner>();
        return services;
    }
}
