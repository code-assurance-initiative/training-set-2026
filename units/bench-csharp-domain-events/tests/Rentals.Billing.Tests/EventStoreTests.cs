using Rentals.Billing.Domain.Accounts;
using Rentals.Billing.Domain.Accounts.Events;
using Rentals.Billing.Domain.EventSourcing;
using Rentals.Billing.Infrastructure;
using Rentals.SharedKernel;

namespace Rentals.Billing.Tests;

public sealed class EventStoreTests
{
    private static readonly MemberAccountId Account = new(Guid.Parse("3a1f6c0e-2b7d-4e8a-9c55-71d2e0b4f8a3"));

    private static ChargePosted Charge(decimal amount) =>
        new ChargePosted(Account, new ChargeId(Guid.NewGuid()), new Money(amount, "EUR"), "Damage", DateTimeOffset.UnixEpoch);

    [Fact]
    public async Task An_append_at_a_stale_version_is_refused()
    {
        var store = new InMemoryEventStore();
        await store.AppendAsync(Account.StreamId, 0, [Charge(1m)], null, TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ConcurrencyException>(() =>
            store.AppendAsync(Account.StreamId, 0, [Charge(2m)], null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task An_inbox_receipt_is_recorded_with_the_append_and_only_once()
    {
        var store = new InMemoryEventStore();
        var receipt = new InboxReceipt("billing.test", Guid.NewGuid());

        await store.AppendAsync(Account.StreamId, 0, [Charge(1m)], receipt, TestContext.Current.CancellationToken);

        Assert.True(await store.HasProcessedAsync(receipt.Consumer, receipt.MessageId, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ConcurrencyException>(() =>
            store.AppendAsync(Account.StreamId, 1, [Charge(1m)], receipt, TestContext.Current.CancellationToken));
        Assert.Single(await store.ReadStreamAsync(Account.StreamId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task The_log_is_read_in_append_order_from_a_position()
    {
        var store = new InMemoryEventStore();
        await store.AppendAsync(Account.StreamId, 0, [Charge(1m), Charge(2m), Charge(3m)], null, TestContext.Current.CancellationToken);

        var page = await store.ReadAllAsync(1, 10, TestContext.Current.CancellationToken);

        Assert.Equal([2L, 3L], page.Select(e => e.Position));
    }
}
