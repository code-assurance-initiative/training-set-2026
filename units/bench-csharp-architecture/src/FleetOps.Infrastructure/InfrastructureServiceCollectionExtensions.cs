using FleetOps.Application.Abstractions;
using FleetOps.Domain.Common;
using FleetOps.Domain.Inspections;
using FleetOps.Domain.Vehicles;
using FleetOps.Domain.WorkOrders;
using FleetOps.Infrastructure.Auditing;
using FleetOps.Infrastructure.Caching;
using FleetOps.Infrastructure.Documents;
using FleetOps.Infrastructure.Email;
using FleetOps.Infrastructure.FuelCards;
using FleetOps.Infrastructure.Geocoding;
using FleetOps.Infrastructure.Parts;
using FleetOps.Infrastructure.Persistence;
using FleetOps.Infrastructure.Reporting;
using FleetOps.Infrastructure.Scheduling;
using FleetOps.Infrastructure.Storage;
using FleetOps.Infrastructure.Tyres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FleetOps.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddFleetInfrastructure(this IServiceCollection services)
    {
        services.BindOptions<DatabaseOptions>(DatabaseOptions.SectionName);
        services.BindOptions<EmailOptions>(EmailOptions.SectionName);
        services.BindOptions<SchedulingOptions>(SchedulingOptions.SectionName);
        services.BindOptions<DocumentOptions>(DocumentOptions.SectionName);
        services.BindOptions<AuditOptions>(AuditOptions.SectionName);
        services.BindOptions<StorageOptions>(StorageOptions.SectionName);
        services.BindOptions<FuelCardOptions>(FuelCardOptions.SectionName);
        services.BindOptions<TyreVendorOptions>(TyreVendorOptions.SectionName);
        services.BindOptions<PartsSupplierOptions>(PartsSupplierOptions.SectionName);
        services.BindOptions<GeocodingOptions>(GeocodingOptions.SectionName);

        // Persistence
        services.AddSingleton<AuditSaveChangesInterceptor>();
        services.AddDbContext<FleetOpsDbContext>((provider, options) => options
            .UseSqlite(provider.GetRequiredService<IOptions<DatabaseOptions>>().Value.ConnectionString)
            .AddInterceptors(provider.GetRequiredService<AuditSaveChangesInterceptor>()));
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<FleetOpsDbContext>());
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IWorkOrderRepository, WorkOrderRepository>();
        services.AddScoped<IInspectionRepository, InspectionRepository>();
        services.AddScoped<IFleetReadModel, FleetReadModel>();
        services.AddScoped<IVehicleReadModel>(provider => provider.GetRequiredService<IFleetReadModel>());
        services.AddScoped<IWorkOrderReadModel>(provider => provider.GetRequiredService<IFleetReadModel>());
        services.AddScoped<IInspectionReadModel>(provider => provider.GetRequiredService<IFleetReadModel>());
        services.AddHostedService<DatabaseInitializer>();

        // E-mail and reminders
        services.AddSingleton<IEmailSender, PickupDirectoryEmailSender>();
        services.AddSingleton(provider =>
        {
            var scheduling = provider.GetRequiredService<IOptions<SchedulingOptions>>().Value;
            return new QuietHours(scheduling.QuietHoursStart, scheduling.QuietHoursEnd);
        });
        services.AddScoped<ReminderMailer>();
        services.AddScoped<ReminderDispatcher>();
        services.AddTransient<IReminderWindowCalculator, ReminderWindowCalculator>();
        services.AddSingleton<MaintenanceReminderPlanner>();
        services.AddSingleton<ReminderScheduler>();

        // Documents and storage
        services.AddSingleton<DocumentNameBuilder>();
        services.AddScoped<WorkOrderCsvExporter>();
        services.AddScoped<InspectionCsvExporter>();
        services.AddSingleton<IDocumentStore, FileSystemDocumentStore>();

        // Fuel cards
        services.AddHttpClient<FuelCardClient>((provider, http) =>
            http.BaseAddress = provider.GetRequiredService<IOptions<FuelCardOptions>>().Value.BaseAddress);
        services.AddScoped<IFuelCardClient>(provider => new RetryingFuelCardClient(
            provider.GetRequiredService<FuelCardClient>(),
            provider.GetRequiredService<IOptions<FuelCardOptions>>(),
            provider.GetRequiredService<TimeProvider>()));
        services.AddScoped<IFuelCardTransactionStore, FuelCardTransactionStore>();
        services.AddScoped<FuelCardImportService>();
        services.AddSingleton<FuelPriceCache>(provider => new FuelPriceCache(
            provider.GetRequiredService<IFuelCardTransactionStore>(),
            provider.GetRequiredService<TimeProvider>()));

        // Tyres and parts
        services.AddHttpClient<ITyreVendorClient, TyreVendorClient>((provider, http) =>
            http.BaseAddress = provider.GetRequiredService<IOptions<TyreVendorOptions>>().Value.BaseAddress);
        services.AddSingleton<TyrePriceCache>();
        services.AddHttpClient<IPartsSupplierClient, PartsSupplierClient>((provider, http) =>
            http.BaseAddress = provider.GetRequiredService<IOptions<PartsSupplierOptions>>().Value.BaseAddress);
        services.AddSingleton<PartsCatalogueSnapshot>();
        services.AddScoped<PartsCatalogueRefresher>();

        // Geocoding
        services.AddSingleton<GeocodingCache>();
        services.AddHttpClient<IGeocoder, HttpGeocoder>((provider, http) =>
            http.BaseAddress = provider.GetRequiredService<IOptions<GeocodingOptions>>().Value.BaseAddress);

        // Auditing, reporting, caching
        services.AddScoped<IAuditTrail, AuditTrail>();
        services.AddScoped<IFleetReporting, FleetReporting>();
        services.AddSingleton<ICacheInvalidator, MemoryCacheInvalidator>();
        return services;
    }

    private static void BindOptions<TOptions>(this IServiceCollection services, string section)
        where TOptions : class =>
        services.AddOptions<TOptions>().BindConfiguration(section).ValidateDataAnnotations();
}
