using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Grades.Application.Abstractions;
using NIMI.UniPortal.Modules.Grades.Application.Common;
using NIMI.UniPortal.Modules.Grades.Domain;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Grades.Application.Drafting;

/// <summary>
/// P9.1, rule R2: only the group's teaching staff, only roster students (each once), and only while every touched
/// grade is still a draft (409 otherwise: nothing is saved). Letters come from the subject faculty's scale (R5).
/// Shared by typed drafts and CSV imports, so both follow exactly the same rules.
/// </summary>
internal sealed class GradeDrafter(
    IGradesDbContext db,
    GradingAccess access,
    GradeScales scales,
    GroupGradesPresenter presenter,
    IAcademicLifecycle lifecycle,
    TimeProvider clock)
{
    /// <summary>The group, after checking that the caller teaches it.</summary>
    public async Task<GradedGroup> TeachingGroupAsync(Guid groupId, Viewer caller, CancellationToken cancellationToken)
    {
        var graded = await access.LoadGroupAsync(groupId, cancellationToken);
        await access.EnsureTeacherAsync(graded, caller, cancellationToken);
        return graded;
    }

    public async Task<GroupGradesDto> DraftAsync(
        GradedGroup graded, IReadOnlyList<GradeEntry> entries, Viewer caller, CancellationToken cancellationToken)
    {
        var roster = await lifecycle.GetStudentsInGroupAsync(graded.Group.Id, cancellationToken);
        var onRoster = roster.ToHashSet();
        var seen = new HashSet<Guid>();
        for (var i = 0; i < entries.Count; i++)
        {
            if (!onRoster.Contains(entries[i].StudentId))
                throw ValidationErrors.For($"entries[{i}].studentId", "This student is not on the group's roster.");
            if (!seen.Add(entries[i].StudentId))
                throw ValidationErrors.For($"entries[{i}].studentId", "This student is listed more than once.");
        }

        var grades = await GroupGrades.LoadTrackedAsync(db, graded, roster, cancellationToken);
        if (entries.Any(e => grades.TryGetValue(e.StudentId, out var grade) && grade.Status != GradeStatus.Draft))
            throw new ConflictException("Some of these grades are already confirmed or published and can't be drafted again.");

        var scale = await scales.ForSubjectAsync(graded.Subject, cancellationToken);
        var now = clock.GetUtcNow();
        foreach (var entry in entries)
        {
            var band = scale.Resolve(entry.Score);
            if (grades.TryGetValue(entry.StudentId, out var existing))
            {
                existing.Redraft(entry.Score, band, caller.UserId, now);
                continue;
            }

            var draft = FinalGrade.Draft(
                entry.StudentId,
                graded.Subject.Id,
                graded.Group.Id,
                graded.Group.TermId,
                graded.Subject.CreditHours,
                entry.Score,
                band,
                caller.UserId,
                now);
            db.FinalGrades.Add(draft);
            grades[entry.StudentId] = draft;
        }

        return await presenter.PresentAsync(graded, roster, grades, cancellationToken);
    }
}
