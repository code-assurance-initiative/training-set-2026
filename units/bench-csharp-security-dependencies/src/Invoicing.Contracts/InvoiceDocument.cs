namespace Invoicing.Contracts;

/// <summary>Everything the renderers need to produce one invoice, in the seller's currency.</summary>
public sealed class InvoiceDocument
{
    public InvoiceDocument(
        string number,
        DateTime issueDate,
        DateTime dueDate,
        string currency,
        Party seller,
        Party buyer,
        IReadOnlyList<InvoiceLine> lines,
        string paymentReference)
    {
        Number = string.IsNullOrWhiteSpace(number) ? throw new ArgumentException("An invoice needs a number.", nameof(number)) : number;
        IssueDate = issueDate.Date;
        DueDate = dueDate.Date >= IssueDate ? dueDate.Date : throw new ArgumentOutOfRangeException(nameof(dueDate), "Due date precedes issue date.");
        Currency = currency?.Length == 3
            ? currency.ToUpperInvariant()
            : throw new ArgumentException("Currencies are ISO 4217 codes.", nameof(currency));
        Seller = seller ?? throw new ArgumentNullException(nameof(seller));
        Buyer = buyer ?? throw new ArgumentNullException(nameof(buyer));
        Lines = lines is { Count: > 0 } ? lines : throw new ArgumentException("An invoice needs at least one line.", nameof(lines));
        PaymentReference = paymentReference ?? throw new ArgumentNullException(nameof(paymentReference));
    }

    public string Number { get; }

    public DateTime IssueDate { get; }

    public DateTime DueDate { get; }

    public string Currency { get; }

    public Party Seller { get; }

    public Party Buyer { get; }

    public IReadOnlyList<InvoiceLine> Lines { get; }

    public string PaymentReference { get; }

    public decimal NetTotal => Lines.Sum(line => line.NetAmount);

    public decimal VatTotal => Lines.Sum(line => line.VatAmount);

    public decimal GrossTotal => NetTotal + VatTotal;
}
