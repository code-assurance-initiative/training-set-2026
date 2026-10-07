using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ReportDesk.Api.Attachments;
using ReportDesk.Api.Shares;

namespace ReportDesk.UnitTests.Shares;

public sealed class ShareStorageTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    [Fact]
    public async Task CleanupJobPurgesExpiredLinks()
    {
        var store = new RecordingShareStore();
        using var provider = new ServiceCollection().AddSingleton<IShareStore>(store).BuildServiceProvider();
        var now = new DateTimeOffset(2026, 10, 7, 6, 0, 0, TimeSpan.Zero);
        using var job = new ShareLinkCleanupJob(
            provider.GetRequiredService<IServiceScopeFactory>(), new FixedClock(now), NullLogger<ShareLinkCleanupJob>.Instance);

        Assert.Equal(3, await job.PurgeExpiredAsync(TestContext.Current.CancellationToken));
        Assert.Equal(now, store.PurgedBefore);
    }

    [Fact]
    public async Task AttachmentsAreReadFromTheDocumentsDirectory()
    {
        var options = TestOptions.Storage(_temp.Path);
        var documentId = Guid.NewGuid();
        var directory = Directory.CreateDirectory(Path.Combine(options.Value.AttachmentsRoot, documentId.ToString("N")));
        await File.WriteAllBytesAsync(Path.Combine(directory.FullName, "scan.pdf"), [1, 2, 3], TestContext.Current.CancellationToken);
        var store = new AttachmentStore(options);

        Assert.Equal([1, 2, 3], await store.ReadAsync(documentId, "scan.pdf", TestContext.Current.CancellationToken));
        Assert.Null(await store.ReadAsync(documentId, "missing.pdf", TestContext.Current.CancellationToken));
    }

    public void Dispose() => _temp.Dispose();

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class RecordingShareStore : IShareStore
    {
        public DateTimeOffset? PurgedBefore { get; private set; }

        public Task AddAsync(ShareLink link, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<ShareLink?> FindAsync(string token, CancellationToken cancellationToken) => Task.FromResult<ShareLink?>(null);

        public Task<int> PurgeExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            PurgedBefore = now;
            return Task.FromResult(3);
        }
    }
}
