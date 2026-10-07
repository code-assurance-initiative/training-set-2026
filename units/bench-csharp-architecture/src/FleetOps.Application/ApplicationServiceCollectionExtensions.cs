using FleetOps.Application.Facades;
using FleetOps.Application.Features.WorkOrders;
using FleetOps.Application.Telemetry;
using FleetOps.Domain.Maintenance;
using Microsoft.Extensions.DependencyInjection;

namespace FleetOps.Application;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Application services. The hosts add the mediator itself (its source generator runs in the host).</summary>
    public static IServiceCollection AddFleetApplication(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddMetrics();
        services.AddSingleton<FleetMetrics>();
        services.AddScoped<MaintenanceDueEvaluator>();
        services.AddScoped<OpenWorkOrderHandler>();
        services.AddScoped<IWorkOrderFacade, WorkOrderFacade>();
        return services;
    }
}
