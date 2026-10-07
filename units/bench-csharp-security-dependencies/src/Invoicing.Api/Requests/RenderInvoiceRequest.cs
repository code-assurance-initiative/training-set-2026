using System.ComponentModel.DataAnnotations;
using Invoicing.Contracts;

namespace Invoicing.Api.Requests;

public sealed class RenderInvoiceRequest : IValidatableObject
{
    [Required, RegularExpression("^[A-Z0-9][A-Z0-9-]{0,31}$")]
    public string Number { get; init; } = string.Empty;

    public DateOnly IssueDate { get; init; }

    public DateOnly DueDate { get; init; }

    [Required, RegularExpression("^[A-Z]{3}$")]
    public string Currency { get; init; } = string.Empty;

    [Required]
    public PartyRequest Seller { get; init; } = new();

    [Required]
    public PartyRequest Buyer { get; init; } = new();

    [Required, MinLength(1), MaxLength(200)]
    public IReadOnlyList<InvoiceLineRequest> Lines { get; init; } = [];

    [Required, StringLength(35, MinimumLength = 1)]
    public string PaymentReference { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DueDate < IssueDate)
        {
            yield return new ValidationResult("The due date precedes the issue date.", [nameof(DueDate)]);
        }
    }

    public InvoiceDocument ToDocument() =>
        new(
            Number,
            IssueDate.ToDateTime(TimeOnly.MinValue),
            DueDate.ToDateTime(TimeOnly.MinValue),
            Currency,
            Seller.ToParty(),
            Buyer.ToParty(),
            [.. Lines.Select(line => line.ToLine())],
            PaymentReference);
}
