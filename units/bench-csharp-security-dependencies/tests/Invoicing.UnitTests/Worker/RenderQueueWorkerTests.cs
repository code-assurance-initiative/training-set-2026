using FluentAssertions;
using Invoicing.UnitTests.TestSupport;
using Invoicing.Worker.Jobs;
using Invoicing.Worker.Mail;
using Invoicing.Worker.Processing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Invoicing.UnitTests.Worker;

public sealed class RenderQueueWorkerTests
{
    [Fact]
    public async Task DrainsTheQueueOnEachScheduledOccurrence()
    {
        var start = new DateTimeOffset(2026, 10, 7, 9, 0, 30, TimeSpan.Zero);
        var clock = new FakeTimeProvider(start);
        var store = new RecordingStore(clock);
        var processor = new RenderQueueProcessor(store, Renderers.Pdf(), new NoMailer(), NullLogger<RenderQueueProcessor>.Instance);
        using var worker = new RenderQueueWorker(processor, Options.Create(new WorkerOptions { Schedule = "*/2 * * * *", BatchSize = 5 }), clock, NullLogger<RenderQueueWorker>.Instance);

        await worker.StartAsync(TestContext.Current.CancellationToken);
        await AdvanceUntilAsync(clock, () => store.ClaimedAt.Count >= 2);
        await worker.StopAsync(TestContext.Current.CancellationToken);

        // Fake time moves in steps, so a claim is observed at or shortly after its occurrence, never before it.
        store.ClaimedAt[0].Should().BeOnOrAfter(start.AddSeconds(90));
        store.ClaimedAt[1].Should().BeOnOrAfter(start.AddSeconds(210));
    }

    /// <summary>Moves fake time forward in ten-second steps, yielding so the worker can observe each step.</summary>
    private static async Task AdvanceUntilAsync(FakeTimeProvider clock, Func<bool> done)
    {
        for (var step = 0; step < 200 && !done(); step++)
        {
            await Task.Delay(5, TestContext.Current.CancellationToken);
            clock.Advance(TimeSpan.FromSeconds(10));
        }
    }

    private sealed class RecordingStore(TimeProvider clock) : IRenderJobStore
    {
        private readonly Lock _gate = new();
        private readonly List<DateTimeOffset> _claimedAt = [];

        public IReadOnlyList<DateTimeOffset> ClaimedAt
        {
            get
            {
                lock (_gate)
                {
                    return [.. _claimedAt];
                }
            }
        }

        public Task<IReadOnlyList<RenderJob>> ClaimAsync(int batchSize, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                _claimedAt.Add(clock.GetUtcNow());
            }

            return Task.FromResult<IReadOnlyList<RenderJob>>([]);
        }

        public Task MarkSentAsync(long jobId, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task MarkFailedAsync(long jobId, string reason, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class NoMailer : IInvoiceMailer
    {
        public Task SendAsync(string recipient, Invoicing.Contracts.InvoiceDocument invoice, byte[] pdf, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
