namespace Rentals.Billing.Application.Projections;

/// <summary>The read model behind the account page: one row per member account.</summary>
public sealed class AccountBalanceView
{
    public Guid AccountId { get; set; }

    public string Currency { get; set; } = "EUR";

    public decimal Charged { get; set; }

    public decimal Paid { get; set; }

    public decimal Outstanding => Charged - Paid;

    public decimal DepositsHeld { get; set; }

    public int OpenLoans { get; set; }

    public decimal? LastPaymentAmount { get; set; }

    public DateTimeOffset? LastPaymentAt { get; set; }
}
