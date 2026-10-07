using Microsoft.EntityFrameworkCore;
using Rentals.Billing.Infrastructure;
using Rentals.Lending.Infrastructure;
using Rentals.Lending.Infrastructure.Messaging;
using Rentals.Lending.Infrastructure.Persistence;
using Rentals.Messaging;

namespace Rentals.Worker;

/// <summary>The worker's composition root: both contexts, the in-process bus and the two background loops.</summary>
public static class RentalsHost
{
    public static HostApplicationBuilder AddRentals(this HostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var lendingDatabase = builder.Configuration.GetConnectionString("Lending")
            ?? throw new InvalidOperationException("ConnectionStrings:Lending is not configured.");

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddInProcessMessaging();
        builder.Services.AddLending(options => options.UseSqlite(lendingDatabase));
        builder.Services.AddBilling();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<OutboxDispatcher>());
        builder.Services.AddHostedService(sp => sp.GetRequiredService<ProjectionRunner>());
        return builder;
    }

    /// <summary>Brings the Lending database to the latest migration.</summary>
    public static async Task MigrateLendingDatabaseAsync(this IHost host, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(host);
        var scope = host.Services.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var db = scope.ServiceProvider.GetRequiredService<LendingDbContext>();
            await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
