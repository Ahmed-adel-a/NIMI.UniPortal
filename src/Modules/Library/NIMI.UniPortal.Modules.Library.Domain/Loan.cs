namespace NIMI.UniPortal.Modules.Library.Domain;

/// <summary>A copy lent to a member: due after the role's loan period, renewable before the due date within a limit.</summary>
public sealed class Loan
{
    private Loan()
    {
    }

    public Guid Id { get; private set; }

    public Guid CopyId { get; private set; }

    public Guid BookId { get; private set; }

    /// <summary>Users user id (cross-schema id, no FK).</summary>
    public Guid UserId { get; private set; }

    public DateTimeOffset BorrowedAt { get; private set; }

    public DateTimeOffset DueAt { get; private set; }

    public DateTimeOffset? ReturnedAt { get; private set; }

    public int Renewals { get; private set; }

    public bool IsOpen => ReturnedAt is null;

    public static Loan Open(Guid copyId, Guid bookId, Guid userId, DateTimeOffset now, int loanDays) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            CopyId = copyId,
            BookId = bookId,
            UserId = userId,
            BorrowedAt = now,
            DueAt = now.AddDays(loanDays),
        };

    public bool IsOverdue(DateTimeOffset now) => IsOpen && now > DueAt;

    /// <summary>A new loan period from today: only while open, not overdue, and under the role's renewal limit.</summary>
    public void Renew(DateTimeOffset now, int loanDays, int maxRenewals)
    {
        if (!IsOpen)
            throw new InvalidOperationException("This loan was returned.");
        if (IsOverdue(now))
            throw new InvalidOperationException("An overdue loan can't be renewed; please return the book.");
        if (Renewals >= maxRenewals)
            throw new InvalidOperationException($"At most {maxRenewals} renewal(s).");

        Renewals++;
        DueAt = now.AddDays(loanDays);
    }

    public void Return(DateTimeOffset now)
    {
        if (!IsOpen)
            throw new InvalidOperationException("This loan was already returned.");

        ReturnedAt = now;
    }
}
