using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rentals.Billing.Application.Payments;
using Rentals.Contracts.IntegrationEvents;
using Rentals.Lending.Application.Loans;
using Rentals.Lending.Application.Members;
using Rentals.Messaging;
using Rentals.Worker;

namespace Rentals.IntegrationTests;

/// <summary>The worker's composition root resolves every handler both contexts register, on a migrated database.</summary>
public sealed class WorkerCompositionTests
{
    [Fact]
    public async Task The_worker_composes_both_contexts_and_migrates_the_database()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = "Testing" });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Lending"] = "Data Source=worker-composition;Mode=Memory;Cache=Shared",
        });
        using var host = builder.AddRentals().Build();
        using var keepAlive = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=worker-composition;Mode=Memory;Cache=Shared");
        await keepAlive.OpenAsync(TestContext.Current.CancellationToken);

        await host.MigrateLendingDatabaseAsync(TestContext.Current.CancellationToken);

        await using var scope = host.Services.CreateAsyncScope();
        Assert.Equal(2, scope.ServiceProvider.GetServices<ICommandHandler<ExtendLoanCommand>>().Count());
        Assert.NotNull(scope.ServiceProvider.GetService<ICommandHandler<RegisterMemberCommand>>());
        Assert.NotNull(scope.ServiceProvider.GetService<ICommandHandler<RecordPaymentCommand>>());
        Assert.Equal(2, scope.ServiceProvider.GetServices<IIntegrationEventHandler<EquipmentDamageReportedIntegrationEvent>>().Count());
        Assert.Equal(2, host.Services.GetServices<IHostedService>().Count());
    }
}
