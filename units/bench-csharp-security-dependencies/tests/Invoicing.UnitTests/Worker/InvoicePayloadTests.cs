using System.Text.Json;
using FluentAssertions;
using Invoicing.Worker.Jobs;

namespace Invoicing.UnitTests.Worker;

public sealed class InvoicePayloadTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private const string StoredPayload = """
        {
          "number": "INV-2026-0107",
          "issueDate": "2026-10-01T00:00:00",
          "dueDate": "2026-10-31T00:00:00",
          "currency": "EUR",
          "seller": { "name": "Nordlys Software ApS", "vatId": "DK31415926", "addressLines": ["Havnegade 4", "8000 Aarhus C"], "countryCode": "DK" },
          "buyer": { "name": "Fjord Logistics A/S", "vatId": "", "addressLines": ["Industrivej 12"], "countryCode": "DK" },
          "lines": [ { "description": "Licence, annual", "quantity": 1, "unitPrice": 18000, "vatRate": 0.25 } ],
          "paymentReference": "RF18539007547034"
        }
        """;

    [Fact]
    public void TheStoredPayloadBecomesAnInvoice()
    {
        var payload = JsonSerializer.Deserialize<InvoicePayload>(StoredPayload, Web);

        var invoice = payload?.ToDocument();

        invoice.Should().NotBeNull();
        invoice?.Number.Should().Be("INV-2026-0107");
        invoice?.Seller.AddressLines.Should().Equal("Havnegade 4", "8000 Aarhus C");
        invoice?.GrossTotal.Should().Be(22500m);
    }

    [Fact]
    public void APayloadWithoutLinesIsRejected()
    {
        var payload = new InvoicePayload { Number = "INV-1", IssueDate = new DateTime(2026, 1, 1), DueDate = new DateTime(2026, 1, 2), Currency = "EUR", Seller = Party(), Buyer = Party(), PaymentReference = "r" };

        var act = payload.ToDocument;

        act.Should().Throw<ArgumentException>().WithParameterName("lines");
    }

    private static InvoicePayload.PartyPayload Party() => new() { Name = "Acme", AddressLines = ["Main Street 1"], CountryCode = "DK" };
}
