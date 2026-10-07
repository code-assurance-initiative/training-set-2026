using Microsoft.Extensions.DependencyInjection;
using Warehouse.Stock.Application.Catalog;
using Warehouse.Stock.Application.Inventory;
using Warehouse.Stock.Infrastructure.InMemory;
using Warehouse.Stock.Infrastructure.Workers;

namespace Warehouse.Stock.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Registers the in-memory stores (ADR 0002) and the reservation expiry worker.</summary>
    public static IServiceCollection AddStockInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ICatalogStore, InMemoryCatalogStore>();
        services.AddSingleton<IInventoryStore, InMemoryInventoryStore>();
        services.AddHostedService<ReservationExpiryWorker>();
        return services;
    }
}
