using FluentAssertions;
using Invoicing.UnitTests.TestSupport;
using Invoicing.Worker.Mail;
using MimeKit;

namespace Invoicing.UnitTests.Worker;

public sealed class SmtpInvoiceMailerTests
{
    [Fact]
    public void TheMessageCarriesThePdfAndThePaymentDetails()
    {
        var settings = new SmtpOptions { Host = "relay.test", FromAddress = "invoices@nordlys.example", FromName = "Nordlys Billing" };
        var invoice = Invoices.Invoice();

        var message = SmtpInvoiceMailer.Compose(settings, "ap@fjord.example", invoice, [0x25, 0x50, 0x44, 0x46]);

        message.Subject.Should().Be($"Invoice INV-2026-0042 from {invoice.Seller.Name}");
        message.To.Mailboxes.Should().ContainSingle().Which.Address.Should().Be("ap@fjord.example");
        message.Attachments.OfType<MimePart>().Should().ContainSingle().Which.FileName.Should().Be("INV-2026-0042.pdf");
        message.TextBody.Should().Contain("1,725.63 EUR").And.Contain("RF18539007547034");
    }
}
