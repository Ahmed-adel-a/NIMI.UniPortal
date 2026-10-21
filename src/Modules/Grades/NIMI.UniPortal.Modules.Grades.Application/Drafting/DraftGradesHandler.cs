using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Grades.Application.Abstractions;
using NIMI.UniPortal.Modules.Grades.Application.Common;
using NIMI.UniPortal.Modules.Grades.Domain;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Grades.Application.Drafting;

/// <summary>
/// P9.1 — rule R2: only the group's teaching staff, only roster students (each once), and only while every touched
/// grade is still a draft (409 otherwise — nothing is saved). Letters come from the subject faculty's scale (R5).
/// </summary>
internal sealed class DraftGradesHandler(
    IGradesDbContext db,
    GradingAccess access,
    GradeScales scales,
    GroupGradesPresenter presenter,
    IAcademicLifecycle lifecycle,
    TimeProvider clock)
    : IRequestHandler<DraftGradesCommand, GroupGradesDto>
{
    public async Task<GroupGradesDto> Handle(DraftGradesCommand command, CancellationToken cancellationToken)
    {
        var graded = await access.LoadGroupAsync(command.GroupId, cancellationToken);
        await access.EnsureTeacherAsync(graded, command.Caller, cancellationToken);

        var roster = await lifecycle.GetStudentsInGroupAsync(graded.Group.Id, cancellationToken);
        var onRoster = roster.ToHashSet();
        var entries = command.Entries!;
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
                existing.Redraft(entry.Score, band, command.Caller.UserId, now);
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
                command.Caller.UserId,
                now);
            db.FinalGrades.Add(draft);
            grades[entry.StudentId] = draft;
        }

        return await presenter.PresentAsync(graded, roster, grades, cancellationToken);
    }
}
