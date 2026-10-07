namespace Rentals.Billing.Application.Payments;

public sealed record RecordPaymentCommand(Guid MemberId, decimal Amount, string Currency, string PaymentReference);
