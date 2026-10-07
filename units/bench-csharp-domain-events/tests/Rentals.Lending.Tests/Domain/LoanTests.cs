using Rentals.Lending.Domain.Catalogue;
using Rentals.Lending.Domain.Loans;
using Rentals.Lending.Domain.Members;
using Rentals.SharedKernel;

namespace Rentals.Lending.Tests.Domain;

public sealed class LoanTests
{
    private static readonly DateTimeOffset Start = new(2026, 4, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Money Rate = new(4m, "EUR");
    private static readonly Money Replacement = new(180m, "EUR");

    private static Loan OpenLoan(int days = 7)
    {
        var member = Member.Register("Grace Hopper", "grace@example.org", "", Start.AddDays(-30));
        return Loan.Open(member, EquipmentId.New(), EquipmentUnitId.New(), LoanPeriod.Starting(Start, days));
    }

    [Fact]
    public void Open_raises_loan_opened_with_the_due_date()
    {
        var loan = OpenLoan();

        var opened = Assert.IsType<LoanOpened>(Assert.Single(loan.DomainEvents));
        Assert.Equal(Start.AddDays(7), opened.DueAt);
        Assert.Equal(LoanStatus.Open, loan.Status);
    }

    [Fact]
    public void A_loan_period_is_one_to_28_days()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LoanPeriod.Starting(Start, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => LoanPeriod.Starting(Start, 29));
    }

    [Fact]
    public void Extend_moves_the_due_date_and_repeating_it_changes_nothing()
    {
        var loan = OpenLoan();
        loan.ClearDomainEvents();

        loan.Extend(Start.AddDays(10), Start.AddDays(5));
        loan.Extend(Start.AddDays(10), Start.AddDays(5));

        Assert.Equal(Start.AddDays(10), loan.DueAt);
        Assert.Equal(1, loan.ExtensionCount);
        Assert.Single(loan.DomainEvents);
    }

    [Fact]
    public void Extend_is_limited()
    {
        var loan = OpenLoan();
        loan.Extend(Start.AddDays(10), Start);
        loan.Extend(Start.AddDays(14), Start);

        Assert.Throws<DomainRuleViolationException>(() => loan.Extend(Start.AddDays(20), Start));
    }

    [Fact]
    public void Extend_cannot_move_the_due_date_earlier()
    {
        var loan = OpenLoan();

        Assert.Throws<ArgumentOutOfRangeException>(() => loan.Extend(Start.AddDays(3), Start));
    }

    [Fact]
    public void Return_records_the_condition_once()
    {
        var loan = OpenLoan();
        loan.ClearDomainEvents();

        loan.Return(UnitCondition.Damaged, Start.AddDays(9), Replacement, Rate);
        loan.Return(UnitCondition.Good, Start.AddDays(10), Replacement, Rate);

        var returned = Assert.IsType<LoanReturned>(Assert.Single(loan.DomainEvents));
        Assert.Equal(UnitCondition.Damaged, returned.ReturnCondition);
        Assert.Equal(Start.AddDays(9), loan.ReturnedAt);
    }

    [Fact]
    public void IsOverdueAt_is_true_only_for_an_open_loan_past_its_due_date()
    {
        var loan = OpenLoan();

        Assert.False(loan.IsOverdueAt(Start.AddDays(7)));
        Assert.True(loan.IsOverdueAt(Start.AddDays(8)));
        loan.Return(UnitCondition.Good, Start.AddDays(8), Replacement, Rate);
        Assert.False(loan.IsOverdueAt(Start.AddDays(9)));
    }
}
