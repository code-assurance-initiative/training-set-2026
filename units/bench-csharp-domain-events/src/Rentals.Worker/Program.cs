using Rentals.Worker;

using var host = Host.CreateApplicationBuilder(args).AddRentals().Build();
await host.MigrateLendingDatabaseAsync(CancellationToken.None).ConfigureAwait(false);
await host.RunAsync().ConfigureAwait(false);
