namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Plans;

/// <summary>One catalog subject in a department's study plan: taught in a level and a term.</summary>
public sealed class StudyPlanSubject
{
    private StudyPlanSubject()
    {
    }

    public Guid Id { get; private set; }

    public Guid StudyPlanId { get; private set; }

    public int Level { get; private set; }

    public PlanTerm Term { get; private set; }

    public Guid SubjectId { get; private set; }

    internal static StudyPlanSubject Create(Guid studyPlanId, int level, PlanTerm term, Guid subjectId) => new()
    {
        Id = Guid.CreateVersion7(),
        StudyPlanId = studyPlanId,
        Level = level,
        Term = term,
        SubjectId = subjectId,
    };
}
