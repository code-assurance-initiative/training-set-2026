using Microsoft.EntityFrameworkCore;
using Rentals.SharedKernel;

namespace Rentals.Lending.Domain.Members;

/// <summary>A person registered with the tool library who may borrow equipment.</summary>
[Index(nameof(Email), IsUnique = true)]
public sealed class Member : AggregateRoot<MemberId>
{
    private const int StandardLoanLimit = 2;
    private const int PlusLoanLimit = 5;

    private Member(MemberId id)
        : base(id)
    {
    }

    public string FullName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public MemberStatus Status { get; private set; }

    public MembershipTier Tier { get; set; }

    public int LoanLimit { get; private set; }

    public int LoansTaken { get; private set; }

    public DateTimeOffset MembershipExpiresAt { get; private set; }

    public string? SuspensionReason { get; private set; }

    public static Member Register(string fullName, string email, string phone, DateTimeOffset registeredAt)
    {
        var member = new Member(MemberId.New())
        {
            Status = MemberStatus.Active,
            MembershipExpiresAt = registeredAt.AddYears(1),
        };
        member.ChangeContactDetails(fullName, email, phone);
        member.ChangeTier(MembershipTier.Standard);
        member.Raise(new MemberRegistered(member.Id, registeredAt));
        return member;
    }

    public void ChangeContactDetails(string fullName, string email, string phone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentNullException.ThrowIfNull(phone);
        if (!email.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("An e-mail address contains '@'.", nameof(email));
        }

        FullName = fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = phone.Trim();
    }

    public bool HasSameContact(string fullName, string email, string phone) =>
        string.Equals(FullName, fullName.Trim(), StringComparison.Ordinal)
        && string.Equals(Email, email.Trim(), StringComparison.OrdinalIgnoreCase)
        && string.Equals(Phone, phone.Trim(), StringComparison.Ordinal);

    public void ChangeTier(MembershipTier tier)
    {
        Tier = tier;
        LoanLimit = tier == MembershipTier.Plus ? PlusLoanLimit : StandardLoanLimit;
    }

    public void RecordCheckout() => LoansTaken++;

    public void Suspend(string reason, DateTimeOffset suspendedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status == MemberStatus.Closed)
        {
            throw new DomainRuleViolationException("A closed membership cannot be suspended.");
        }

        if (Status == MemberStatus.Suspended)
        {
            return;
        }

        Status = MemberStatus.Suspended;
        SuspensionReason = reason;
        Raise(new MemberSuspended(Id, reason, suspendedAt));
    }

    public void Reinstate(DateTimeOffset reinstatedAt)
    {
        if (Status != MemberStatus.Suspended)
        {
            return;
        }

        Status = MemberStatus.Active;
        SuspensionReason = null;
        Raise(new MemberReinstated(Id, reinstatedAt));
    }

    public void RenewMembership()
    {
        var now = DateTimeOffset.UtcNow;
        var from = MembershipExpiresAt > now ? MembershipExpiresAt : now;
        MembershipExpiresAt = from.AddYears(1);
        Raise(new MembershipRenewed(Id, MembershipExpiresAt, now));
    }

    public void ErasePersonalData(DateTimeOffset erasedAt)
    {
        if (Status == MemberStatus.Closed)
        {
            return;
        }

        FullName = "(erased)";
        Email = $"erased-{Id}@erased.invalid";
        Phone = string.Empty;
        SuspensionReason = null;
        Status = MemberStatus.Closed;
        Raise(new MemberPersonalDataErased(Id, erasedAt));
    }
}
