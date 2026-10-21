namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;

/// <summary>
/// One passed/failed subject record per student and term (P9 writes it). P4.5 reads <see cref="Passed"/> to decide
/// whether a prerequisite is satisfied and <see cref="CreditHours"/> for earned credits — the table stays empty
/// until grades exist.
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
}
