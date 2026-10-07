using Invoicing.Contracts;

namespace Invoicing.Worker.Jobs;

/// <summary>The JSON the billing system stores in <c>render_jobs.payload</c>.</summary>
public sealed class InvoicePayload
{
    public string Number { get; init; } = string.Empty;

    public DateTime IssueDate { get; init; }

    public DateTime DueDate { get; init; }

    public string Currency { get; init; } = string.Empty;

    public PartyPayload Seller { get; init; } = new();

    public PartyPayload Buyer { get; init; } = new();

    public IReadOnlyList<LinePayload> Lines { get; init; } = [];

    public string PaymentReference { get; init; } = string.Empty;

    public InvoiceDocument ToDocument() =>
        new(Number, IssueDate, DueDate, Currency, Seller.ToParty(), Buyer.ToParty(), [.. Lines.Select(l => l.ToLine())], PaymentReference);

    public sealed class PartyPayload
    {
        public string Name { get; init; } = string.Empty;

        public string VatId { get; init; } = string.Empty;

        public IReadOnlyList<string> AddressLines { get; init; } = [];

        public string CountryCode { get; init; } = string.Empty;

        public Party ToParty() => new(Name, VatId, AddressLines, CountryCode);
    }

    public sealed class LinePayload
    {
        public string Description { get; init; } = string.Empty;

        public decimal Quantity { get; init; }

        public decimal UnitPrice { get; init; }

        public decimal VatRate { get; init; }

        public InvoiceLine ToLine() => new(Description, Quantity, UnitPrice, VatRate);
    }
}
