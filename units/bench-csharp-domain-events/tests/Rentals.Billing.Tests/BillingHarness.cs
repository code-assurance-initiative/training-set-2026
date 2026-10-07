using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Rentals.Billing.Application.Handlers;
using Rentals.Billing.Domain.Accounts;
using Rentals.Billing.Infrastructure;
using Rentals.Contracts.IntegrationEvents;
using Rentals.Messaging;

namespace Rentals.Billing.Tests;

/// <summary>The Billing context with its in-memory store, a fake clock and a stub catalogue service.</summary>
public sealed class BillingHarness : IAsyncDisposable
{
    public static readonly DateTimeOffset Now = new(2026, 5, 4, 8, 0, 0, TimeSpan.Zero);
    public static readonly Guid MemberId = Guid.Parse("0c9b7a52-81d3-4c11-8e3a-5f0d6b2e9a10");

    private BillingHarness(ServiceProvider services, StubCatalogue catalogue)
    {
        Services = services;
        Catalogue = catalogue;
    }

    public ServiceProvider Services { get; }

    public StubCatalogue Catalogue { get; }

    public static async Task<BillingHarness> StartAsync()
    {
        var catalogue = new StubCatalogue();
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddSingleton<TimeProvider>(new FakeTimeProvider(Now))
            .AddInProcessMessaging()
            .AddBilling();
        services.AddHttpClient<LoanOpenedHandler>().ConfigurePrimaryHttpMessageHandler(() => catalogue);
        var harness = new BillingHarness(services.BuildServiceProvider(validateScopes: true), catalogue);
        await harness.DeliverAsync(new MemberRegisteredIntegrationEvent(MemberId, "Ada Lovelace", "ada@example.org", Now));
        return harness;
    }

    public async Task DeliverAsync<TEvent>(TEvent integrationEvent, Guid? messageId = null)
        where TEvent : IIntegrationEvent
    {
        var context = new MessageContext(messageId ?? Guid.NewGuid(), typeof(TEvent).Name, Now);
        var scope = Services.CreateAsyncScope();
        await using (scope)
        {
            foreach (var handler in scope.ServiceProvider.GetServices<IIntegrationEventHandler<TEvent>>())
            {
                await handler.HandleAsync(integrationEvent, context, TestContext.Current.CancellationToken);
            }
        }
    }

    public async Task SendAsync<TCommand>(TCommand command)
        where TCommand : notnull
    {
        var scope = Services.CreateAsyncScope();
        await using (scope)
        {
            await scope.ServiceProvider.GetRequiredService<ICommandDispatcher>().DispatchAsync(command, TestContext.Current.CancellationToken);
        }
    }

    public async Task<MemberAccount> AccountAsync()
    {
        var scope = Services.CreateAsyncScope();
        await using (scope)
        {
            return await scope.ServiceProvider.GetRequiredService<IMemberAccountRepository>()
                .LoadAsync(new MemberAccountId(MemberId), TestContext.Current.CancellationToken)
                ?? throw new InvalidOperationException("No account.");
        }
    }

    public ValueTask DisposeAsync() => Services.DisposeAsync();
}

/// <summary>Answers the catalogue's valuation endpoint with a fixed replacement value and counts the calls.</summary>
public sealed class StubCatalogue : HttpMessageHandler
{
    public int Calls { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        var id = request.RequestUri?.Segments[^2].TrimEnd('/') ?? string.Empty;
        var body = $$"""{"equipmentId":"{{Guid.Parse(id)}}","replacementValue":180.00,"currency":"EUR"}""";
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });
    }
}
