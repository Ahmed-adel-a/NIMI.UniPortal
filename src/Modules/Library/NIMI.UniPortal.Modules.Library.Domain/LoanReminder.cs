namespace NIMI.UniPortal.Modules.Library.Domain;

/// <summary>A reminder sent for a loan on one day — unique per loan, kind and day, so a re-run never repeats it.</summary>
public sealed class LoanReminder
{
    private LoanReminder()
    {
    }

    public Guid Id { get; private set; }

    public Guid LoanId { get; private set; }

    public ReminderKind Kind { get; private set; }

    public DateOnly SentOn { get; private set; }

    public static LoanReminder Record(Guid loanId, ReminderKind kind, DateOnly sentOn) =>
        new() { Id = Guid.CreateVersion7(), LoanId = loanId, Kind = kind, SentOn = sentOn };
}
