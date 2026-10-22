using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Standing;

/// <summary>
/// A student's place in the semester-based system: department, year level (فرقة), how many academic years they have
/// spent in it and their status. A cohort is simply every standing of one department and level.
/// </summary>
public sealed class StudentStanding
{
    private StudentStanding()
    {
    }

    public Guid Id { get; private set; }

    public Guid StudentId { get; private set; }

    public Guid DepartmentId { get; private set; }

    public int Level { get; private set; }

    /// <summary>1 in the first year at this level; a repeat adds one.</summary>
    public int YearsInLevel { get; private set; }

    public StandingStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Takes part in term enrollment (graduated, withdrawn and held students do not).</summary>
    public bool IsEnrolling => Status is StandingStatus.Active or StandingStatus.Repeating or StandingStatus.External;

    /// <summary>Only the failed subjects are taken (repeating or external); otherwise the level's plan as well.</summary>
    public bool TakesFailedSubjectsOnly => Status is StandingStatus.Repeating or StandingStatus.External;

    public static StudentStanding Place(Guid studentId, Guid departmentId, int level, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        StudentId = studentId,
        DepartmentId = departmentId,
        Level = AcademicLevels.Valid(level, nameof(level)),
        YearsInLevel = 1,
        Status = StandingStatus.Active,
        CreatedAt = now,
        UpdatedAt = now,
    };

    /// <summary>Student Affairs places the student in a level (e.g. a transfer): a fresh, active first year there.</summary>
    public void MoveTo(Guid departmentId, int level, DateTimeOffset now)
    {
        DepartmentId = departmentId;
        Level = AcademicLevels.Valid(level, nameof(level));
        YearsInLevel = 1;
        Status = StandingStatus.Active;
        UpdatedAt = now;
    }

    /// <summary>Applies a year-end decision (rule or override).</summary>
    public void Apply(PromotionOutcome outcome, DateTimeOffset now)
    {
        switch (outcome.Result)
        {
            case PromotionResult.Promoted:
            case PromotionResult.PromotedWithCarried:
                Level = AcademicLevels.Valid(outcome.ToLevel, nameof(outcome));
                YearsInLevel = 1;
                Status = StandingStatus.Active;
                break;
            case PromotionResult.Repeat:
                YearsInLevel++;
                Status = StandingStatus.Repeating;
                break;
            case PromotionResult.External:
                YearsInLevel++;
                Status = StandingStatus.External;
                break;
            case PromotionResult.Graduated:
                Status = StandingStatus.Graduated;
                break;
            case PromotionResult.Held:
                Status = StandingStatus.OnHold;
                break;
        }

        UpdatedAt = now;
    }
}
