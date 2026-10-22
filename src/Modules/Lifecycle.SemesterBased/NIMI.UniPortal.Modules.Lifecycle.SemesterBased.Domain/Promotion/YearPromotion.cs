namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;

/// <summary>
/// One year-end decision for one student: automatic (no <see cref="DecidedBy"/>) or a Student Affairs override.
/// A held decision is followed by the real one once the fees are paid; the history keeps both.
/// </summary>
public sealed class YearPromotion
{
    private YearPromotion()
    {
    }

    public Guid Id { get; private set; }

    public Guid StudentId { get; private set; }

    public Guid DepartmentId { get; private set; }

    /// <summary>The academic year that ended (e.g. 2026 for 2026/2027).</summary>
    public int AcademicYear { get; private set; }

    public int FromLevel { get; private set; }

    public int ToLevel { get; private set; }

    public PromotionResult Result { get; private set; }

    public int FailedSubjects { get; private set; }

    public string Reason { get; private set; } = null!;

    public DateTimeOffset DecidedAt { get; private set; }

    /// <summary>The Student Affairs member who overrode the rule; null when the rule decided.</summary>
    public Guid? DecidedBy { get; private set; }

    public static YearPromotion Record(
        Guid studentId, Guid departmentId, int academicYear, int fromLevel, PromotionOutcome outcome, int failedSubjects,
        Guid? decidedBy, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        StudentId = studentId,
        DepartmentId = departmentId,
        AcademicYear = academicYear,
        FromLevel = fromLevel,
        ToLevel = outcome.ToLevel,
        Result = outcome.Result,
        FailedSubjects = failedSubjects,
        Reason = outcome.Reason,
        DecidedAt = now,
        DecidedBy = decidedBy,
    };
}
