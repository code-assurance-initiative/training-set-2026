using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Rentals.Contracts.IntegrationEvents;
using Rentals.Lending.Application.Catalogue;
using Rentals.Lending.Application.Members;
using Rentals.Lending.Domain.Catalogue;
using Rentals.Lending.Domain.Members;
using Rentals.Lending.Infrastructure;
using Rentals.Lending.Infrastructure.Persistence;
using Rentals.Messaging;
using Rentals.SharedKernel;

namespace Rentals.Lending.Tests.Application;

/// <summary>The Lending context on an in-memory SQLite database, with a fake clock and a recording subscriber.</summary>
public sealed class LendingHarness : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private LendingHarness(SqliteConnection connection, ServiceProvider services)
    {
        _connection = connection;
        Services = services;
    }

    public static DateTimeOffset Now { get; } = new(2026, 5, 4, 8, 0, 0, TimeSpan.Zero);

    public ServiceProvider Services { get; }

    public FakeTimeProvider Clock => (FakeTimeProvider)Services.GetRequiredService<TimeProvider>();

    public RecordingSubscriber Published => Services.GetRequiredService<RecordingSubscriber>();

    public static async Task<LendingHarness> StartAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddSingleton<TimeProvider>(new FakeTimeProvider(Now))
            .AddInProcessMessaging()
            .AddLending(options => options.UseSqlite(connection))
            .AddSingleton<RecordingSubscriber>()
            .AddSingleton<IIntegrationEventHandler<MemberSuspendedIntegrationEvent>>(sp => sp.GetRequiredService<RecordingSubscriber>())
            .AddSingleton<IIntegrationEventHandler<LoanOpenedIntegrationEvent>>(sp => sp.GetRequiredService<RecordingSubscriber>())
            .BuildServiceProvider(validateScopes: true);
        var harness = new LendingHarness(connection, services);
        await harness.InScopeAsync<LendingDbContext>(db => db.Database.MigrateAsync(TestContext.Current.CancellationToken));
        return harness;
    }

    public async Task SendAsync<TCommand>(TCommand command)
        where TCommand : notnull
    {
        var scope = Services.CreateAsyncScope();
        await using (scope)
        {
            await scope.ServiceProvider.GetRequiredService<ICommandHandler<TCommand>>()
                .HandleAsync(command, TestContext.Current.CancellationToken);
        }
    }

    public async Task<TResult> QueryAsync<TQuery, TResult>(TQuery query)
        where TQuery : notnull
    {
        var scope = Services.CreateAsyncScope();
        await using (scope)
        {
            return await scope.ServiceProvider.GetRequiredService<IQueryHandler<TQuery, TResult>>()
                .HandleAsync(query, TestContext.Current.CancellationToken);
        }
    }

    public async Task InScopeAsync<TService>(Func<TService, Task> action)
        where TService : notnull
    {
        var scope = Services.CreateAsyncScope();
        await using (scope)
        {
            await action(scope.ServiceProvider.GetRequiredService<TService>());
        }
    }

    public async Task<TResult> InScopeAsync<TService, TResult>(Func<TService, Task<TResult>> action)
        where TService : notnull
    {
        var scope = Services.CreateAsyncScope();
        await using (scope)
        {
            return await action(scope.ServiceProvider.GetRequiredService<TService>());
        }
    }

    public async Task<Member> RegisterMemberAsync(string email = "ada@example.org")
    {
        await SendAsync(new RegisterMemberCommand("Ada Lovelace", email, "+44 20 7946 0000"));
        return await InScopeAsync<IMemberRepository, Member>(async members =>
            await members.FindByEmailAsync(email, TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("The member was not saved."));
    }

    public async Task<Equipment> RegisterDrillAsync(params string[] serials)
    {
        var id = EquipmentId.New();
        await SendAsync(new RegisterEquipmentCommand(
            id, "Cordless drill", new Money(4m, "EUR"), new Money(180m, "EUR"), serials.Length == 0 ? ["DR-001"] : serials));
        return await InScopeAsync<IEquipmentRepository, Equipment>(async equipment =>
            await equipment.GetAsync(id, TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("The equipment was not saved."));
    }

    public async Task<IReadOnlyList<string>> OutboxTypesAsync() =>
        await InScopeAsync<LendingDbContext, List<string>>(db =>
            db.OutboxMessages.OrderBy(m => m.OccurredAt).Select(m => m.Type).ToListAsync(TestContext.Current.CancellationToken));

    public async ValueTask DisposeAsync()
    {
        await Services.DisposeAsync();
        await _connection.DisposeAsync();
    }
}

public sealed class RecordingSubscriber
    : IIntegrationEventHandler<MemberSuspendedIntegrationEvent>, IIntegrationEventHandler<LoanOpenedIntegrationEvent>
{
    private readonly List<(IIntegrationEvent Event, MessageContext Context)> _received = [];

    public IReadOnlyList<(IIntegrationEvent Event, MessageContext Context)> Received => _received;

    public Task HandleAsync(MemberSuspendedIntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken) =>
        Record(integrationEvent, context);

    public Task HandleAsync(LoanOpenedIntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken) =>
        Record(integrationEvent, context);

    private Task Record(IIntegrationEvent integrationEvent, MessageContext context)
    {
        _received.Add((integrationEvent, context));
        return Task.CompletedTask;
    }
}
