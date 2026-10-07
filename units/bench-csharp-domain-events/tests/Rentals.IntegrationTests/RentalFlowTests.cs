using System.Net;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Rentals.Billing.Application.Handlers;
using Rentals.Billing.Application.Projections;
using Rentals.Billing.Domain.Accounts;
using Rentals.Billing.Infrastructure;
using Rentals.Lending.Application.Catalogue;
using Rentals.Lending.Application.Loans;
using Rentals.Lending.Application.Members;
using Rentals.Lending.Domain.Catalogue;
using Rentals.Lending.Domain.Loans;
using Rentals.Lending.Domain.Members;
using Rentals.Lending.Infrastructure;
using Rentals.Lending.Infrastructure.Messaging;
using Rentals.Lending.Infrastructure.Persistence;
using Rentals.Messaging;
using Rentals.SharedKernel;

namespace Rentals.IntegrationTests;

/// <summary>Both contexts in one process: Lending's outbox feeds Billing through the in-process bus.</summary>
public sealed class RentalFlowTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Start = new(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly FakeTimeProvider _clock = new(Start);
    private ServiceProvider? _services;

    private ServiceProvider Services => _services ?? throw new InvalidOperationException("Not initialised.");

    public async ValueTask InitializeAsync()
    {
        await _connection.OpenAsync(TestContext.Current.CancellationToken);
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddSingleton<TimeProvider>(_clock)
            .AddInProcessMessaging()
            .AddLending(options => options.UseSqlite(_connection))
            .AddBilling();
        services.AddHttpClient<LoanOpenedHandler>().ConfigurePrimaryHttpMessageHandler(() => new CatalogueStub());
        _services = services.BuildServiceProvider(validateScopes: true);
        var scope = Services.CreateAsyncScope();
        await using (scope)
        {
            await scope.ServiceProvider.GetRequiredService<LendingDbContext>().Database.MigrateAsync(TestContext.Current.CancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_services is not null)
        {
            await _services.DisposeAsync();
        }

        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task A_late_return_reaches_the_member_account_through_the_outbox()
    {
        await SendAsync(new RegisterMemberCommand("Grace Hopper", "grace@example.org", "+1 202 555 0143"));
        var equipmentId = EquipmentId.New();
        await SendAsync(new RegisterEquipmentCommand(equipmentId, "Hedge trimmer", new Money(5m, "EUR"), new Money(250m, "EUR"), ["HT-100"]));
        var member = await InScopeAsync<IMemberRepository, Member?>(m => m.FindByEmailAsync("grace@example.org", TestContext.Current.CancellationToken));
        var equipment = await InScopeAsync<IEquipmentRepository, Equipment?>(e => e.GetAsync(equipmentId, TestContext.Current.CancellationToken));
        Assert.NotNull(member);
        Assert.NotNull(equipment);

        await SendAsync(new CheckoutEquipmentCommand(member.Id, equipment.Units.Single().Id, 7));
        await DispatchAsync();
        var loan = Assert.Single(await InScopeAsync<ILoanRepository, IReadOnlyList<Loan>>(l => l.ListOpenAsync(TestContext.Current.CancellationToken)));

        _clock.Advance(TimeSpan.FromDays(9));
        await SendAsync(new ReturnEquipmentCommand(loan.Id, UnitCondition.Good));
        await DispatchAsync();

        var account = await InScopeAsync<IMemberAccountRepository, MemberAccount?>(a =>
            a.LoadAsync(new MemberAccountId(member.Id.Value), TestContext.Current.CancellationToken));
        Assert.NotNull(account);
        Assert.Empty(account.HeldDeposits);
        Assert.Equal(15m, account.Outstanding);

        await Services.GetRequiredService<ProjectionRunner>().CatchUpAsync(TestContext.Current.CancellationToken);
        var view = Services.GetRequiredService<IAccountBalanceViewStore>().Find(member.Id.Value);
        Assert.Equal(15m, view?.Outstanding);
    }

    [Fact]
    public async Task A_damaged_return_charges_the_member_and_withdraws_the_unit()
    {
        await SendAsync(new RegisterMemberCommand("Grace Hopper", "grace@example.org", ""));
        var equipmentId = EquipmentId.New();
        await SendAsync(new RegisterEquipmentCommand(equipmentId, "Hedge trimmer", new Money(5m, "EUR"), new Money(250m, "EUR"), ["HT-100"]));
        var member = await InScopeAsync<IMemberRepository, Member?>(m => m.FindByEmailAsync("grace@example.org", TestContext.Current.CancellationToken));
        var unit = (await InScopeAsync<IEquipmentRepository, Equipment?>(e => e.GetAsync(equipmentId, TestContext.Current.CancellationToken)))?.Units.Single();
        Assert.NotNull(member);
        Assert.NotNull(unit);
        await SendAsync(new CheckoutEquipmentCommand(member.Id, unit.Id, 7));
        var loan = Assert.Single(await InScopeAsync<ILoanRepository, IReadOnlyList<Loan>>(l => l.ListOpenAsync(TestContext.Current.CancellationToken)));

        await SendAsync(new ReturnEquipmentCommand(loan.Id, UnitCondition.Damaged));
        await DispatchAsync();

        var account = await InScopeAsync<IMemberAccountRepository, MemberAccount?>(a =>
            a.LoadAsync(new MemberAccountId(member.Id.Value), TestContext.Current.CancellationToken));
        Assert.Equal(125m, account?.Outstanding);
        var equipment = await InScopeAsync<IEquipmentRepository, Equipment?>(e => e.GetAsync(equipmentId, TestContext.Current.CancellationToken));
        Assert.Equal(UnitCondition.Damaged, equipment?.Units.Single().Condition);
    }

    private async Task SendAsync<TCommand>(TCommand command)
        where TCommand : notnull
    {
        var scope = Services.CreateAsyncScope();
        await using (scope)
        {
            await scope.ServiceProvider.GetRequiredService<ICommandDispatcher>().DispatchAsync(command, TestContext.Current.CancellationToken);
        }
    }

    private async Task<TResult> InScopeAsync<TService, TResult>(Func<TService, Task<TResult>> action)
        where TService : notnull
    {
        var scope = Services.CreateAsyncScope();
        await using (scope)
        {
            return await action(scope.ServiceProvider.GetRequiredService<TService>());
        }
    }

    private Task<int> DispatchAsync() =>
        Services.GetRequiredService<OutboxDispatcher>().DispatchPendingAsync(TestContext.Current.CancellationToken);

    private sealed class CatalogueStub : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"equipmentId":"00000000-0000-0000-0000-000000000000","replacementValue":250.00,"currency":"EUR"}""", Encoding.UTF8, "application/json"),
            });
    }
}
