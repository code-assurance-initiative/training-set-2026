namespace Rentals.Billing.Domain.Accounts;

/// <summary>Billing's reference to a loan in Lending: the loan's id as published, nothing more.</summary>
public readonly record struct LoanReference(Guid Value)
{
    public override string ToString() => Value.ToString("N");
}
