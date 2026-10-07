using Rentals.Lending.Domain.Members;
using Rentals.SharedKernel;

namespace Rentals.Lending.Tests.Domain;

public sealed class MemberTests
{
    private static readonly DateTimeOffset RegisteredAt = new(2026, 3, 2, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Register_normalises_contact_details_and_raises_registered()
    {
        var member = Member.Register(" Ada Lovelace ", "Ada@Example.ORG", "+44 20 7946 0000", RegisteredAt);

        Assert.Equal("Ada Lovelace", member.FullName);
        Assert.Equal("ada@example.org", member.Email);
        Assert.Equal(MemberStatus.Active, member.Status);
        Assert.Equal(2, member.LoanLimit);
        Assert.Equal(RegisteredAt.AddYears(1), member.MembershipExpiresAt);
        Assert.IsType<MemberRegistered>(Assert.Single(member.DomainEvents));
    }

    [Fact]
    public void Register_rejects_an_address_without_at_sign() =>
        Assert.Throws<ArgumentException>(() => Member.Register("Ada", "ada.example.org", "", RegisteredAt));

    [Fact]
    public void HasSameContact_ignores_case_of_email_and_surrounding_spaces()
    {
        var member = Member.Register("Ada Lovelace", "ada@example.org", "123", RegisteredAt);

        Assert.True(member.HasSameContact("Ada Lovelace ", "ADA@example.org", " 123"));
        Assert.False(member.HasSameContact("Ada Byron", "ada@example.org", "123"));
    }

    [Fact]
    public void ChangeTier_adjusts_the_loan_limit()
    {
        var member = Member.Register("Ada", "ada@example.org", "", RegisteredAt);

        member.ChangeTier(MembershipTier.Plus);

        Assert.Equal(5, member.LoanLimit);
    }

    [Fact]
    public void Suspend_twice_raises_one_event()
    {
        var member = Member.Register("Ada", "ada@example.org", "", RegisteredAt);
        member.ClearDomainEvents();

        member.Suspend("Unpaid fees", RegisteredAt.AddDays(1));
        member.Suspend("Unpaid fees", RegisteredAt.AddDays(2));

        Assert.Equal(MemberStatus.Suspended, member.Status);
        Assert.IsType<MemberSuspended>(Assert.Single(member.DomainEvents));
    }

    [Fact]
    public void Reinstate_returns_a_suspended_member_to_active()
    {
        var member = Member.Register("Ada", "ada@example.org", "", RegisteredAt);
        member.Suspend("Unpaid fees", RegisteredAt.AddDays(1));

        member.Reinstate(RegisteredAt.AddDays(3));

        Assert.Equal(MemberStatus.Active, member.Status);
        Assert.Null(member.SuspensionReason);
    }

    [Fact]
    public void A_closed_membership_cannot_be_suspended()
    {
        var member = Member.Register("Ada", "ada@example.org", "", RegisteredAt);
        member.ErasePersonalData(RegisteredAt.AddDays(1));

        Assert.Throws<DomainRuleViolationException>(() => member.Suspend("x", RegisteredAt.AddDays(2)));
    }

    [Fact]
    public void ErasePersonalData_removes_name_email_and_phone()
    {
        var member = Member.Register("Ada Lovelace", "ada@example.org", "123", RegisteredAt);

        member.ErasePersonalData(RegisteredAt.AddDays(1));

        Assert.Equal("(erased)", member.FullName);
        Assert.DoesNotContain("ada", member.Email, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(member.Phone);
        Assert.Equal(MemberStatus.Closed, member.Status);
    }

    [Fact]
    public void RenewMembership_extends_an_unexpired_membership_by_a_year()
    {
        var member = Member.Register("Ada", "ada@example.org", "", DateTimeOffset.UtcNow);
        var before = member.MembershipExpiresAt;

        member.RenewMembership();

        Assert.Equal(before.AddYears(1), member.MembershipExpiresAt);
    }
}
