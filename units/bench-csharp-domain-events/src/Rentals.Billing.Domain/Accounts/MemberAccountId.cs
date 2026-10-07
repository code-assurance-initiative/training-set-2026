namespace Rentals.Billing.Domain.Accounts;

/// <summary>An account is keyed by the member it bills; the value is the member's id as Lending publishes it.</summary>
public readonly record struct MemberAccountId(Guid Value)
{
    public string StreamId => $"member-account-{Value:N}";

    public override string ToString() => Value.ToString("N");
}
