using Microsoft.Extensions.DependencyInjection;
using Rentals.Billing.Application.Payments;
using Rentals.Billing.Application.Projections;
using Rentals.Billing.Infrastructure;
using Rentals.Contracts.IntegrationEvents;
using Rentals.Lending.Domain.Members;

namespace Rentals.Billing.Tests;

public sealed class ProjectionTests
{
    [Fact]
    public async Task The_balance_view_follows_the_account_streams()
    {
        await using var billing = await BillingHarness.StartAsync();
        var loanId = Guid.NewGuid();
        await billing.DeliverAsync(new LoanOpenedIntegrationEvent(loanId, BillingHarness.MemberId, Guid.NewGuid(), BillingHarness.Now.AddDays(7), BillingHarness.Now));
        await billing.SendAsync(new RecordPaymentCommand(BillingHarness.MemberId, 5m, "EUR", "PAY-1"));

        var projected = await billing.Services.GetRequiredService<ProjectionRunner>().CatchUpAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, projected);
        var view = billing.Services.GetRequiredService<IAccountBalanceViewStore>().Find(BillingHarness.MemberId);
        Assert.NotNull(view);
        Assert.Equal(36m, view.DepositsHeld);
        Assert.Equal(1, view.OpenLoans);
        Assert.Equal(-5m, view.Outstanding);
        Assert.Equal(5m, view.LastPaymentAmount);
    }

    [Fact]
    public async Task Catching_up_twice_projects_each_event_once()
    {
        await using var billing = await BillingHarness.StartAsync();
        var runner = billing.Services.GetRequiredService<ProjectionRunner>();

        Assert.Equal(1, await runner.CatchUpAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await runner.CatchUpAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task The_query_returns_the_view()
    {
        await using var billing = await BillingHarness.StartAsync();
        await billing.Services.GetRequiredService<ProjectionRunner>().CatchUpAsync(TestContext.Current.CancellationToken);

        var view = await new AccountBalanceQueryHandler(billing.Services.GetRequiredService<IAccountBalanceViewStore>())
            .HandleAsync(new AccountBalanceQuery(new MemberId(BillingHarness.MemberId)), TestContext.Current.CancellationToken);

        Assert.Equal("EUR", view?.Currency);
    }
}
