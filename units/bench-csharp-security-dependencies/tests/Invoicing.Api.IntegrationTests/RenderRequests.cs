namespace Invoicing.Api.IntegrationTests;

public static class RenderRequests
{
    public static object Valid(string number = "INV-2026-0042") => new
    {
        number,
        issueDate = "2026-09-30",
        dueDate = "2026-10-30",
        currency = "EUR",
        seller = new { name = "Nordlys Software ApS", vatId = "DK31415926", addressLines = new[] { "Havnegade 4", "8000 Aarhus C" }, countryCode = "DK" },
        buyer = new { name = "Fjord Logistics A/S", vatId = "", addressLines = new[] { "Industrivej 12", "7100 Vejle" }, countryCode = "DK" },
        lines = new[] { new { description = "Licence, annual", quantity = 1, unitPrice = 18000m, vatRate = 0.25m } },
        paymentReference = "RF18539007547034",
    };

    public const string ErpPayload = """
        {
          "doc": {
            "no": "ERP-778812", "date": "2026-09-28", "due": "2026-10-28", "cur": "DKK", "ref": "+71<000000000778812",
            "seller": { "name": "Nordlys Software ApS", "vat": "DK31415926", "addr": ["Havnegade 4", "8000 Aarhus C"], "country": "DK" },
            "buyer": { "name": "Fjord Logistics A/S", "addr": ["Industrivej 12", "7100 Vejle"], "country": "DK" },
            "rows": [ { "txt": "Licence, annual", "qty": 1, "price": 18000, "vat": 25 } ]
          }
        }
        """;
}
