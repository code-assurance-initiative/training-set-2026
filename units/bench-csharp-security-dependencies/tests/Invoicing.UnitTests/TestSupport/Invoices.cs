using Bogus;
using Invoicing.Contracts;

namespace Invoicing.UnitTests.TestSupport;

/// <summary>
/// Realistic invoices. Each invoice is generated from a seed derived from its number, so the same number always
/// yields the same parties, whatever order (or thread) the tests run in.
/// </summary>
public static class Invoices
{
    public static Party Party(string countryCode = "DK", int seed = 20261007)
    {
        var faker = new Faker("en") { Random = new Randomizer(seed) };
        return new(faker.Company.CompanyName(), "DK" + faker.Random.Number(10000000, 99999999), [faker.Address.StreetAddress(), $"{faker.Address.ZipCode()} {faker.Address.City()}"], countryCode);
    }

    public static InvoiceDocument Invoice(string number = "INV-2026-0042", params InvoiceLine[] lines)
    {
        var seed = number.Aggregate(17, (hash, c) => unchecked((hash * 31) + c));
        return new(
            number,
            new DateTime(2026, 9, 30),
            new DateTime(2026, 10, 30),
            "EUR",
            Party("DK", seed),
            Party("DE", seed + 1),
            lines.Length > 0 ? lines : [new InvoiceLine("Consulting, September", 12, 95m, 0.25m), new InvoiceLine("Travel", 1, 240.5m, 0.25m)],
            "RF18539007547034");
    }
}
