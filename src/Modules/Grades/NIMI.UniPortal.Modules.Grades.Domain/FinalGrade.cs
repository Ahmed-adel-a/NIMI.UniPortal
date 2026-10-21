namespace NIMI.UniPortal.Modules.Grades.Domain;

/// <summary>
/// A student's final grade in one subject of one term (P9, decision D3). The teacher drafts and may redraft; Student
/// Affairs confirms and publishes; after that only an audited correction changes it (R4). Letter, points and credit
/// hours are snapshots, so later catalog or scale edits never rewrite history.
/// </summary>
public sealed class FinalGrade
{
    private FinalGrade()
    {
    }

    public Guid Id { get; private set; }

    public Guid StudentId { get; private set; }

    /// <summary>Scheduling subject id (cross-schema id, no FK).</summary>
    public Guid SubjectId { get; private set; }

    /// <summary>Scheduling subject-group id (cross-schema id, no FK).</summary>
    public Guid SubjectGroupId { get; private set; }

    /// <summary>Scheduling term id (cross-schema id, no FK).</summary>
    public Guid TermId { get; private set; }

    public int CreditHours { get; private set; }

    public decimal Score { get; private set; }

    public string Grade { get; private set; } = string.Empty;

    public decimal GradePoints { get; private set; }

    public GradeStatus Status { get; private set; }

    public Guid DraftedBy { get; private set; }

    public Guid? ConfirmedBy { get; private set; }

    public DateTimeOffset? ConfirmedAt { get; private set; }

    public Guid? PublishedBy { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Passed ⇔ the grade earns points (P09 §6).</summary>
    public bool Passed => GradePoints > 0m;

    public static FinalGrade Draft(
        Guid studentId,
        Guid subjectId,
        Guid subjectGroupId,
        Guid termId,
        int creditHours,
        decimal score,
        GradeBand band,
        Guid draftedBy,
        DateTimeOffset now)
    {
        if (creditHours < 0)
            throw new ArgumentOutOfRangeException(nameof(creditHours), creditHours, "Must be zero or more.");

        var grade = new FinalGrade
        {
            Id = Guid.CreateVersion7(),
            StudentId = studentId,
            SubjectId = subjectId,
            SubjectGroupId = subjectGroupId,
            TermId = termId,
            CreditHours = creditHours,
            Status = GradeStatus.Draft,
            DraftedBy = draftedBy,
            CreatedAt = now,
        };
        grade.SetScore(score, band);
        return grade;
    }

    public void Redraft(decimal score, GradeBand band, Guid draftedBy, DateTimeOffset now)
    {
        if (Status != GradeStatus.Draft)
            throw new InvalidOperationException("Only a draft grade can be redrafted.");

        SetScore(score, band);
        DraftedBy = draftedBy;
        UpdatedAt = now;
    }

    public void Confirm(Guid adminId, DateTimeOffset now)
    {
        if (Status != GradeStatus.Draft)
            throw new InvalidOperationException("Only a draft grade can be confirmed.");

        Status = GradeStatus.Confirmed;
        ConfirmedBy = adminId;
        ConfirmedAt = now;
        UpdatedAt = now;
    }

    public void Publish(Guid adminId, DateTimeOffset now)
    {
        if (Status != GradeStatus.Confirmed)
            throw new InvalidOperationException("Only a confirmed grade can be published.");

        Status = GradeStatus.Published;
        PublishedBy = adminId;
        PublishedAt = now;
        UpdatedAt = now;
    }

    /// <summary>Rule R4 — changes a published grade and returns the audit row to store with it.</summary>
    public GradeCorrection Correct(decimal score, GradeBand band, string reason, Guid adminId, DateTimeOffset now)
    {
        if (Status != GradeStatus.Published)
            throw new InvalidOperationException("Only a published grade can be corrected.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A correction needs a reason.", nameof(reason));

        var trimmed = reason.Trim();
        if (trimmed.Length > GradeCorrection.MaxReasonLength)
            throw new ArgumentException($"At most {GradeCorrection.MaxReasonLength} characters.", nameof(reason));

        var (oldScore, oldGrade) = (Score, Grade);
        SetScore(score, band);
        UpdatedAt = now;
        return GradeCorrection.Record(Id, oldScore, Score, oldGrade, Grade, trimmed, adminId, now);
    }

    private void SetScore(decimal score, GradeBand band)
    {
        if (score is < 0m or > 100m || decimal.Round(score, 2) != score)
            throw new ArgumentOutOfRangeException(nameof(score), score, "Between 0 and 100, at most 2 decimals.");

        Score = score;
        Grade = band.Grade;
        GradePoints = band.Points;
    }
}
