namespace Rentals.Billing.Domain.Accounts;

public readonly record struct ChargeId(Guid Value)
{
    public override string ToString() => Value.ToString("N");
}
