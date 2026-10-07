using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Warehouse.Stock.Application.Catalog;
using Warehouse.Stock.Application.Inventory;
using Warehouse.Stock.Application.Reservations;

namespace Warehouse.Stock.Application;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the application services. They are stateless, so they are singletons; state lives in the stores.
    /// </summary>
    public static IServiceCollection AddStockApplication(this IServiceCollection services)
    {
        services
            .AddOptions<ReservationOptions>()
            .BindConfiguration(ReservationOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<CatalogService>();
        services.AddSingleton<StockService>();
        services.AddSingleton<ReservationService>();
        return services;
    }
}
