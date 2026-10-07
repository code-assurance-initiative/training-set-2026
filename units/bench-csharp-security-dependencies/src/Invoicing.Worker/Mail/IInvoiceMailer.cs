using Invoicing.Contracts;

namespace Invoicing.Worker.Mail;

public interface IInvoiceMailer
{
    Task SendAsync(string recipient, InvoiceDocument invoice, byte[] pdf, CancellationToken cancellationToken);
}
