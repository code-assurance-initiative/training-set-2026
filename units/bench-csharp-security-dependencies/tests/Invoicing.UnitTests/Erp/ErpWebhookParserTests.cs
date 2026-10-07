using FluentAssertions;
using Invoicing.Api.Erp;

namespace Invoicing.UnitTests.Erp;

public sealed class ErpWebhookParserTests
{
    internal const string ValidPayload = """
        {
          "doc": {
            "no": "ERP-778812",
            "date": "2026-09-28",
            "due": "2026-10-28",
            "cur": "dkk",
            "ref": "+71<000000000778812",
            "seller": { "name": "Nordlys Software ApS", "vat": "DK31415926", "addr": ["Havnegade 4", "8000 Aarhus C"], "country": "DK" },
            "buyer": { "name": "Fjord Logistics A/S", "addr": ["Industrivej 12", "7100 Vejle"], "country": "DK" },
            "rows": [
              { "txt": "Licence, annual", "qty": 1, "price": 18000, "vat": 25 },
              { "txt": "Support hours", "qty": 3.5, "price": 950.0, "vat": 25 }
            ]
          }
        }
        """;

    [Fact]
    public void MapsTheLegacyPayloadToAnInvoice()
    {
        var invoice = ErpWebhookParser.Parse(ValidPayload);

        invoice.Number.Should().Be("ERP-778812");
        invoice.Currency.Should().Be("DKK");
        invoice.Buyer.VatId.Should().BeEmpty();
        invoice.Lines.Should().HaveCount(2);
        invoice.Lines[1].VatRate.Should().Be(0.25m);
        invoice.GrossTotal.Should().Be(26656.25m);
    }

    [Fact]
    public void RejectsTextThatIsNotJson()
    {
        var act = () => ErpWebhookParser.Parse("{ doc: ");

        act.Should().Throw<ErpPayloadException>().WithMessage("*not valid JSON*");
    }

    [Fact]
    public void RejectsAPayloadWithoutRows()
    {
        var act = () => ErpWebhookParser.Parse("""{ "doc": { "no": "1", "date": "2026-01-01", "due": "2026-01-02", "cur": "EUR", "ref": "r", "seller": { "name": "a", "addr": [], "country": "DK" }, "buyer": { "name": "b", "addr": [], "country": "DK" } } }""");

        act.Should().Throw<ErpPayloadException>().WithMessage("*'rows'*");
    }

    [Fact]
    public void RejectsAQuantityGivenAsText()
    {
        var payload = ValidPayload.Replace("\"qty\": 1,", "\"qty\": \"1\",", StringComparison.Ordinal);

        var act = () => ErpWebhookParser.Parse(payload);

        act.Should().Throw<ErpPayloadException>().WithMessage("*'qty' must be a number*");
    }
}
