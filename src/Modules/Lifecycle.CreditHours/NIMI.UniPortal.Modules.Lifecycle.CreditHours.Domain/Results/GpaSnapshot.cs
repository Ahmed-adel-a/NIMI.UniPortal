namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Results;

/// <summary>
/// P9.3b — a student's standing after one term: that term's GPA and the cumulative GPA as of it. Recomputed from the
/// credit ledger whenever grades of the student are published or corrected.
/// </summary>
public sealed class GpaSnapshot
{
    private GpaSnapshot()
    {
    }

    public Guid Id { get; private set; }

    public Guid StudentId { get; private set; }

    public Guid TermId { get; private set; }

    /// <summary>The term's start date (snapshot from Scheduling), so "latest" never needs a cross-module lookup.</summary>
    public DateOnly TermStartsOn { get; private set; }

    public decimal? TermGpa { get; private set; }

    public decimal? CumulativeGpa { get; private set; }

    public int CreditsAttempted { get; private set; }

    public int CreditsEarned { get; private set; }

    public DateTimeOffset ComputedAt { get; private set; }

    public static GpaSnapshot Create(Guid studentId, Guid termId, DateOnly termStartsOn) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            StudentId = studentId,
            TermId = termId,
            TermStartsOn = termStartsOn,
        };

    /// <summary>Recomputes from all of the student's ledger lines (every term).</summary>
    public void Recompute(IReadOnlyCollection<ResultLine> allLines, DateTimeOffset now)
    {
        var term = allLines.Where(l => l.TermId == TermId).ToList();
        TermGpa = GpaMath.Of(term);
        CumulativeGpa = GpaMath.Of(GpaMath.LatestAttempts(allLines, TermStartsOn));
        CreditsAttempted = term.Sum(l => l.CreditHours);
        CreditsEarned = term.Where(l => l.Passed).Sum(l => l.CreditHours);
        ComputedAt = now;
    }
}
