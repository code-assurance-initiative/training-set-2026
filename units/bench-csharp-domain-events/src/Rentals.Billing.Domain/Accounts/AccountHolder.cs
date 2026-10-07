namespace Rentals.Billing.Domain.Accounts;

/// <summary>Who an account's statements are addressed to.</summary>
public sealed record AccountHolder
{
    public AccountHolder(string fullName, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        FullName = fullName.Trim();
        Email = email.Trim();
    }

    public string FullName { get; }

    public string Email { get; }
}
