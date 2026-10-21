namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;

/// <summary>
/// One passed/failed subject record per student and term, written from published grades (P9.3b). P4.5 reads
/// <see cref="Passed"/> to decide whether a prerequisite is satisfied and <see cref="CreditHours"/> for earned credits.
/// </summary>
public sealed class CreditLedgerEntry
{
    private CreditLedgerEntry()
    {
    }

    public Guid Id { get; private set; }

    public Guid StudentId { get; private set; }

    public Guid TermId { get; private set; }

    public Guid SubjectId { get; private set; }

    public int CreditHours { get; private set; }

    public bool Passed { get; private set; }

    /// <summary>Grade points on the university's scale (P9); null until a numeric grade is recorded.</summary>
    public decimal? GradePoints { get; private set; }

    public DateTimeOffset RecordedAt { get; private set; }

    /// <summary>P9.3b — a published grade becomes the student's record for that subject and term.</summary>
    public static CreditLedgerEntry Record(
        Guid studentId, Guid termId, Guid subjectId, int creditHours, decimal gradePoints, bool passed, DateTimeOffset now)
    {
        var entry = new CreditLedgerEntry
        {
            Id = Guid.CreateVersion7(),
            StudentId = studentId,
            TermId = termId,
            SubjectId = subjectId,
        };
        entry.Update(creditHours, gradePoints, passed, now);
        return entry;
    }

    /// <summary>A re-published (corrected) grade replaces the row's result — one row per student, term and subject.</summary>
    public void Update(int creditHours, decimal gradePoints, bool passed, DateTimeOffset now)
    {
        if (creditHours < 0)
            throw new ArgumentOutOfRangeException(nameof(creditHours), creditHours, "Must be zero or more.");

        CreditHours = creditHours;
        GradePoints = gradePoints;
        Passed = passed;
        RecordedAt = now;
    }
}
