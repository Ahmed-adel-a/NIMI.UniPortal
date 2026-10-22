using System.Runtime.CompilerServices;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Plans;

/// <summary>
/// A department's programme as Student Affairs sets it: how many year levels it has (the last one graduates) and
/// which catalog subjects are taught in each level and term. Saved as a whole (PUT semantics).
/// </summary>
public sealed class StudyPlan
{
    private readonly List<StudyPlanSubject> _subjects = [];

    private StudyPlan()
    {
    }

    public Guid Id { get; private set; }

    public Guid DepartmentId { get; private set; }

    /// <summary>The number of year levels; the last one is the final (graduating) level.</summary>
    public int Levels { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<StudyPlanSubject> Subjects => _subjects;

    public static StudyPlan Create(Guid departmentId, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        DepartmentId = departmentId,
        Levels = 1,
        UpdatedAt = now,
    };

    /// <summary>Replaces the whole plan. A subject appears once; every level is within the programme.</summary>
    public void Replace(int levels, IReadOnlyList<(int Level, PlanTerm Term, Guid SubjectId)> subjects, DateTimeOffset now)
    {
        Levels = AcademicLevels.Valid(levels, nameof(levels));
        EnsureValid(levels, subjects);

        _subjects.Clear();
        foreach (var (level, term, subjectId) in subjects)
            _subjects.Add(StudyPlanSubject.Create(Id, level, term, subjectId));
        UpdatedAt = now;
    }

    public IEnumerable<StudyPlanSubject> SubjectsFor(int level, PlanTerm term) =>
        _subjects.Where(s => s.Level == level && s.Term == term);

    public StudyPlanSubject? Find(Guid subjectId) => _subjects.FirstOrDefault(s => s.SubjectId == subjectId);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void EnsureValid(int levels, IReadOnlyList<(int Level, PlanTerm Term, Guid SubjectId)> subjects)
    {
        if (subjects.Any(s => s.Level < 1 || s.Level > levels))
            throw new ArgumentOutOfRangeException(nameof(subjects), $"Every subject's level is between 1 and {levels}.");
        if (subjects.Any(s => !Enum.IsDefined(s.Term)))
            throw new ArgumentException("Every subject's term is first or second.", nameof(subjects));
        if (subjects.Select(s => s.SubjectId).Distinct().Count() != subjects.Count)
            throw new ArgumentException("A subject appears only once in a study plan.", nameof(subjects));
    }
}
