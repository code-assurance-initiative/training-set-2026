namespace Rentals.Lending.Domain.Loans;

/// <summary>When a loan starts and when the unit is due back. A loan runs for one to 28 days.</summary>
public sealed record LoanPeriod
{
    public static readonly TimeSpan MaximumLength = TimeSpan.FromDays(28);

    public LoanPeriod(DateTimeOffset start, DateTimeOffset dueAt)
    {
        if (dueAt - start < TimeSpan.FromDays(1) || dueAt - start > MaximumLength)
        {
            throw new ArgumentOutOfRangeException(nameof(dueAt), dueAt, "A loan runs for one to 28 days.");
        }

        Start = start;
        DueAt = dueAt;
    }

    public DateTimeOffset Start { get; }

    public DateTimeOffset DueAt { get; }

    public static LoanPeriod Starting(DateTimeOffset start, int days) => new(start, start.AddDays(days));
}
