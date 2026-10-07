using Invoicing.Contracts;

namespace Invoicing.Worker.Jobs;

/// <summary>A queued request to render an invoice and e-mail it to the buyer's billing address.</summary>
public sealed record RenderJob(long Id, string Recipient, InvoiceDocument Invoice);
