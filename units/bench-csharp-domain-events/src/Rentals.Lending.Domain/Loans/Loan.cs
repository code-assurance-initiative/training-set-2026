using Rentals.Lending.Domain.Catalogue;
using Rentals.Lending.Domain.Members;
using Rentals.SharedKernel;

namespace Rentals.Lending.Domain.Loans;

/// <summary>One unit lent to one member for a period.</summary>
public sealed class Loan : AggregateRoot<LoanId>
{
    public const int MaximumExtensions = 2;

    private Member? _borrower;

    private Loan(LoanId id)
        : base(id)
    {
    }

    public Member Borrower => _borrower ?? throw new InvalidOperationException("The borrower of this loan is not loaded.");

    public EquipmentId EquipmentId { get; private set; }

    public Guid UnitId { get; private set; }

    public DateTimeOffset OpenedAt { get; private set; }

    public DateTimeOffset DueAt { get; private set; }

    public DateTimeOffset? ReturnedAt { get; private set; }

    public LoanStatus Status { get; private set; }

    public int ExtensionCount { get; private set; }

    public static Loan Open(Member borrower, EquipmentId equipmentId, EquipmentUnitId unitId, LoanPeriod period)
    {
        ArgumentNullException.ThrowIfNull(borrower);
        ArgumentNullException.ThrowIfNull(period);
        var loan = new Loan(LoanId.New())
        {
            _borrower = borrower,
            EquipmentId = equipmentId,
            UnitId = unitId.Value,
            OpenedAt = period.Start,
            DueAt = period.DueAt,
            Status = LoanStatus.Open,
        };
        loan.Raise(new LoanOpened(loan.Id, borrower.Id, equipmentId, unitId, period.DueAt, period.Start));
        return loan;
    }

    public void Extend(DateTimeOffset newDueAt, DateTimeOffset requestedAt)
    {
        if (Status != LoanStatus.Open)
        {
            throw new DomainRuleViolationException("Only an open loan can be extended.");
        }

        if (newDueAt == DueAt)
        {
            return;
        }

        if (newDueAt < DueAt || newDueAt - OpenedAt > LoanPeriod.MaximumLength * (MaximumExtensions + 1))
        {
            throw new ArgumentOutOfRangeException(nameof(newDueAt), newDueAt, "An extension moves the due date later, within the limit.");
        }

        if (ExtensionCount == MaximumExtensions)
        {
            throw new DomainRuleViolationException($"A loan can be extended at most {MaximumExtensions} times.");
        }

        DueAt = newDueAt;
        ExtensionCount++;
        Raise(new ExtendLoanEvent(Id, newDueAt, requestedAt));
    }

    public void Return(UnitCondition condition, DateTimeOffset returnedAt, Money replacementValue, Money dailyRate)
    {
        ArgumentNullException.ThrowIfNull(replacementValue);
        ArgumentNullException.ThrowIfNull(dailyRate);
        if (Status == LoanStatus.Returned)
        {
            return;
        }

        Status = LoanStatus.Returned;
        ReturnedAt = returnedAt;
        Raise(new LoanReturned(
            Id, Borrower.Id, EquipmentId, new EquipmentUnitId(UnitId), DueAt, condition, replacementValue, dailyRate, returnedAt));
    }

    public bool IsOverdueAt(DateTimeOffset instant) => Status == LoanStatus.Open && instant > DueAt;
}
