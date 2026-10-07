using System.Xml;
using FluentAssertions;
using Invoicing.Rendering.Ubl;
using Invoicing.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Invoicing.UnitTests.Rendering;

public sealed class SignedUblTests : IDisposable
{
    private readonly System.Security.Cryptography.X509Certificates.X509Certificate2 _certificate = TestCertificates.CreateSigning();

    [Fact]
    public void WritesTheInvoiceIdentityAndTotals()
    {
        var xml = UblInvoiceWriter.Write(Invoices.Invoice());
        var ns = new XmlNamespaceManager(xml.NameTable);
        ns.AddNamespace("cbc", UblInvoiceWriter.BasicNamespace);
        ns.AddNamespace("cac", UblInvoiceWriter.AggregateNamespace);

        xml.SelectSingleNode("/*/cbc:ID", ns)?.InnerText.Should().Be("INV-2026-0042");
        xml.SelectSingleNode("/*/cac:LegalMonetaryTotal/cbc:PayableAmount", ns)?.InnerText.Should().Be("1725.63");
        xml.SelectNodes("/*/cac:InvoiceLine", ns)?.Count.Should().Be(2);
    }

    [Fact]
    public void ASignedInvoiceVerifiesAgainstTheSigningCertificate()
    {
        var signed = Signer().Sign(UblInvoiceWriter.Write(Invoices.Invoice()));

        XmlInvoiceSigner.Verify(signed, _certificate).Should().BeTrue();
    }

    [Fact]
    public void AChangedAmountBreaksTheSignature()
    {
        var signed = Signer().Sign(UblInvoiceWriter.Write(Invoices.Invoice()));
        var ns = new XmlNamespaceManager(signed.NameTable);
        ns.AddNamespace("cbc", UblInvoiceWriter.BasicNamespace);
        ns.AddNamespace("cac", UblInvoiceWriter.AggregateNamespace);

        var payable = signed.SelectSingleNode("/*/cac:LegalMonetaryTotal/cbc:PayableAmount", ns);
        payable.Should().NotBeNull();
        payable?.InnerText = "1.00";

        XmlInvoiceSigner.Verify(signed, _certificate).Should().BeFalse();
    }

    public void Dispose() => _certificate.Dispose();

    private XmlInvoiceSigner Signer() => new(new FixedCertificate(_certificate), NullLogger<XmlInvoiceSigner>.Instance);

    private sealed class FixedCertificate(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) : ISigningCertificateSource
    {
        public System.Security.Cryptography.X509Certificates.X509Certificate2 Current() => certificate;
    }
}
