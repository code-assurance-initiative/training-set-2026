using FluentAssertions;
using Invoicing.Contracts;
using Invoicing.UnitTests.TestSupport;

namespace Invoicing.UnitTests.Contracts;

public sealed class InvoiceDocumentTests
{
    [Fact]
    public void TotalsAreTheSumOfLinesRoundedPerLine()
    {
        var invoice = Invoices.Invoice("INV-1", new InvoiceLine("Widget", 3, 0.335m, 0.25m), new InvoiceLine("Gadget", 1, 10m, 0.07m));

        invoice.NetTotal.Should().Be(11.01m);
        invoice.VatTotal.Should().Be(0.25m + 0.70m);
        invoice.GrossTotal.Should().Be(11.96m);
    }

    [Fact]
    public void ADueDateBeforeTheIssueDateIsRejected()
    {
        var act = () => new InvoiceDocument("INV-2", new DateTime(2026, 9, 30), new DateTime(2026, 9, 1), "EUR", Invoices.Party(), Invoices.Party(), [new InvoiceLine("x", 1, 1, 0)], "ref");

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("dueDate");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ALineNeedsAPositiveQuantity(int quantity)
    {
        var act = () => new InvoiceLine("x", quantity, 1, 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CodesAreNormalisedToUpperCase()
    {
        var party = new Party("Acme", string.Empty, ["Main Street 1"], "dk");

        party.CountryCode.Should().Be("DK");
    }
}
