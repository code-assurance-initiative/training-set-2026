using FluentAssertions;
using Invoicing.Contracts;
using Invoicing.UnitTests.TestSupport;
using Invoicing.Worker.Jobs;
using Invoicing.Worker.Mail;
using Invoicing.Worker.Processing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Invoicing.UnitTests.Worker;

public sealed class RenderQueueProcessorTests
{
    [Fact]
    public async Task SendsEveryClaimedJobAndMarksItSent()
    {
        var store = new FakeStore(Job(1), Job(2));
        var mailer = new FakeMailer();

        var result = await Processor(store, mailer).ProcessBatchAsync(10, TestContext.Current.CancellationToken);

        result.Should().Be(new BatchResult(2, 2));
        store.Sent.Should().Equal(1, 2);
        mailer.Recipients.Should().Equal("billing-1@fjord.example", "billing-2@fjord.example");
    }

    [Fact]
    public async Task AFailingJobIsMarkedFailedAndTheBatchContinues()
    {
        var store = new FakeStore(Job(1), Job(2), Job(3));
        var mailer = new FakeMailer { FailFor = "billing-2@fjord.example" };

        var result = await Processor(store, mailer).ProcessBatchAsync(10, TestContext.Current.CancellationToken);

        result.Should().Be(new BatchResult(3, 2));
        store.Sent.Should().Equal(1, 3);
        store.Failed.Should().ContainSingle().Which.Should().Be((2L, "relay refused the connection"));
    }

    [Fact]
    public async Task AsksTheStoreForTheConfiguredBatchSize()
    {
        var store = new FakeStore();

        await Processor(store, new FakeMailer()).ProcessBatchAsync(25, TestContext.Current.CancellationToken);

        store.RequestedBatchSize.Should().Be(25);
    }

    private static RenderQueueProcessor Processor(IRenderJobStore store, IInvoiceMailer mailer) =>
        new(store, Renderers.Pdf(), mailer, NullLogger<RenderQueueProcessor>.Instance);

    private static RenderJob Job(long id) => new(id, $"billing-{id}@fjord.example", Invoices.Invoice($"INV-{id}"));

    private sealed class FakeStore(params RenderJob[] jobs) : IRenderJobStore
    {
        public List<long> Sent { get; } = [];

        public List<(long, string)> Failed { get; } = [];

        public int RequestedBatchSize { get; private set; }

        public Task<IReadOnlyList<RenderJob>> ClaimAsync(int batchSize, CancellationToken cancellationToken)
        {
            RequestedBatchSize = batchSize;
            return Task.FromResult<IReadOnlyList<RenderJob>>(jobs);
        }

        public Task MarkSentAsync(long jobId, CancellationToken cancellationToken)
        {
            Sent.Add(jobId);
            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(long jobId, string reason, CancellationToken cancellationToken)
        {
            Failed.Add((jobId, reason));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMailer : IInvoiceMailer
    {
        public string? FailFor { get; init; }

        public List<string> Recipients { get; } = [];

        public Task SendAsync(string recipient, InvoiceDocument invoice, byte[] pdf, CancellationToken cancellationToken)
        {
            if (recipient == FailFor)
            {
                throw new IOException("relay refused the connection");
            }

            Recipients.Add(recipient);
            return Task.CompletedTask;
        }
    }
}
