namespace Invoicing.Worker.Jobs;

public interface IRenderJobStore
{
    /// <summary>Claims up to <paramref name="batchSize"/> queued jobs for this worker instance.</summary>
    Task<IReadOnlyList<RenderJob>> ClaimAsync(int batchSize, CancellationToken cancellationToken);

    Task MarkSentAsync(long jobId, CancellationToken cancellationToken);

    Task MarkFailedAsync(long jobId, string reason, CancellationToken cancellationToken);
}
