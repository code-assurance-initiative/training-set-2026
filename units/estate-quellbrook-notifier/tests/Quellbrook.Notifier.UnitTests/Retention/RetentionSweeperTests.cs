using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Quellbrook.Notifier.Persistence;
using Quellbrook.Notifier.Retention;
using Quellbrook.Notifier.UnitTests.TestSupport;

namespace Quellbrook.Notifier.UnitTests.Retention;

public sealed class RetentionSweeperTests : IDisposable
{
    private readonly TestDb _db = new();

    [Fact]
    public async Task ExpiredRecipientsLogEntriesAndMessageIdsAreDeletedAndCurrentOnesKept()
    {
        var now = TestDb.Now;
        using (var context = _db.Context())
        {
            context.Recipients.AddRange(
                Recipient(now.AddDays(-40), completedAt: now.AddDays(-31)),
                Recipient(now.AddDays(-20), completedAt: now.AddDays(-2)),
                Recipient(now.AddDays(-61), completedAt: null),
                Recipient(now.AddDays(-10), completedAt: null));
            context.NotificationLog.AddRange(LogEntry(now.AddDays(-91)), LogEntry(now.AddDays(-89)));
            context.ProcessedMessages.AddRange(
                new ProcessedMessage { MessageId = Guid.NewGuid(), Type = "t", ProcessedAt = now.AddDays(-31) },
                new ProcessedMessage { MessageId = Guid.NewGuid(), Type = "t", ProcessedAt = now.AddDays(-1) });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var sweeper = new RetentionSweeper(_db.Services.GetRequiredService<IServiceScopeFactory>(), _db.Time,
            Options.Create(new RetentionOptions()), NullLogger<RetentionSweeper>.Instance);
        var result = await sweeper.PurgeAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new PurgeResult(2, 1, 1), result);
        using var reading = _db.Context();
        Assert.Equal(2, reading.Recipients.Count());
        Assert.Single(reading.NotificationLog);
        Assert.Single(reading.ProcessedMessages);
    }

    private static Recipient Recipient(DateTimeOffset receivedAt, DateTimeOffset? completedAt) =>
        new() { OrderId = Guid.NewGuid(), Name = "Maja Holm", Email = "maja.holm@post.example", ReceivedAt = receivedAt, CompletedAt = completedAt };

    private static NotificationLogEntry LogEntry(DateTimeOffset sentAt) =>
        new() { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), Kind = "Delivered", Channel = "email", MaskedRecipient = "m***@post.example", SentAt = sentAt };

    public void Dispose() => _db.Dispose();
}
