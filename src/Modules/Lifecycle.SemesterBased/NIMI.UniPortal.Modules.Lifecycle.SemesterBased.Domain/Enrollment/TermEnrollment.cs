namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Enrollment;

/// <summary>
/// A student taking one subject in one group this term: generated from the study plan (or carried after a failure),
/// never self-registered. Every enrollment except a withdrawn one is a roster member. Published grades pass or fail
/// it.
/// </summary>
public sealed class TermEnrollment
{
    private TermEnrollment()
    {
    }

    public Guid Id { get; private set; }

    public Guid StudentId { get; private set; }

    public Guid TermId { get; private set; }

    public Guid SubjectId { get; private set; }

    public Guid SubjectGroupId { get; private set; }

    /// <summary>The plan level the subject belongs to (lower than the student's level when carried).</summary>
    public int Level { get; private set; }

    /// <summary>A snapshot of the subject's credit hours, used for GPA weighting.</summary>
    public int CreditHours { get; private set; }

    /// <summary>A failed subject taken again (carried up, repeated or sat from outside).</summary>
    public bool IsCarried { get; private set; }

    public EnrollmentStatus Status { get; private set; }

    public decimal? GradePoints { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ResultAt { get; private set; }

    public static TermEnrollment Create(
        Guid studentId, Guid termId, Guid subjectId, Guid subjectGroupId, int level, int creditHours, bool isCarried,
        DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        StudentId = studentId,
        TermId = termId,
        SubjectId = subjectId,
        SubjectGroupId = subjectGroupId,
        Level = AcademicLevels.Valid(level, nameof(level)),
        CreditHours = creditHours,
        IsCarried = isCarried,
        Status = EnrollmentStatus.Enrolled,
        CreatedAt = now,
    };

    /// <summary>Student Affairs moves the student to another group of the same subject and term.</summary>
    public void MoveTo(Guid subjectGroupId)
    {
        if (Status != EnrollmentStatus.Enrolled)
            throw new InvalidOperationException("Only an enrollment without a result can change group.");
        SubjectGroupId = subjectGroupId;
    }

    /// <summary>A published (or corrected) grade: the latest one wins.</summary>
    public void RecordResult(bool passed, decimal gradePoints, int creditHours, DateTimeOffset now)
    {
        if (Status == EnrollmentStatus.Withdrawn)
            return;
        Status = passed ? EnrollmentStatus.Passed : EnrollmentStatus.Failed;
        GradePoints = gradePoints;
        CreditHours = creditHours;
        ResultAt = now;
    }
}
