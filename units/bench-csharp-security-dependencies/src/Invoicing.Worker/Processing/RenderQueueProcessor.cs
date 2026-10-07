using Invoicing.Rendering.Pdf;
using Invoicing.Worker.Jobs;
using Invoicing.Worker.Mail;
using MailKit;
using MySql.Data.MySqlClient;

namespace Invoicing.Worker.Processing;

/// <summary>Drains one batch: render, e-mail, record the outcome. One failing job never stops the batch.</summary>
public sealed partial class RenderQueueProcessor(
    IRenderJobStore store,
    PdfInvoiceRenderer renderer,
    IInvoiceMailer mailer,
    ILogger<RenderQueueProcessor> logger)
{
    public async Task<BatchResult> ProcessBatchAsync(int batchSize, CancellationToken cancellationToken)
    {
        var jobs = await store.ClaimAsync(batchSize, cancellationToken).ConfigureAwait(false);
        var sent = 0;
        foreach (var job in jobs)
        {
            if (await TryProcessAsync(job, cancellationToken).ConfigureAwait(false))
            {
                sent++;
            }
        }

        return new BatchResult(jobs.Count, sent);
    }

    private async Task<bool> TryProcessAsync(RenderJob job, CancellationToken cancellationToken)
    {
        try
        {
            var pdf = renderer.Render(job.Invoice, InvoiceBranding.None);
            await mailer.SendAsync(job.Recipient, job.Invoice, pdf, cancellationToken).ConfigureAwait(false);
            await store.MarkSentAsync(job.Id, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex is MailKit.Net.Smtp.SmtpCommandException or ServiceNotConnectedException or IOException or FormatException or MySqlException)
        {
            LogJobFailed(ex, job.Id, job.Invoice.Number);
            await store.MarkFailedAsync(job.Id, ex.Message, cancellationToken).ConfigureAwait(false);
            return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Render job {JobId} (invoice {Number}) failed")]
    private partial void LogJobFailed(Exception exception, long jobId, string number);
}

public readonly record struct BatchResult(int Claimed, int Sent);
