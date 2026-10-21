using FluentValidation;
using FluentValidation.Results;
using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Grades.Application.Abstractions;
using NIMI.UniPortal.Modules.Grades.Application.Common;
using NIMI.UniPortal.Modules.Grades.Domain;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Grades.Application.Publishing;

/// <summary>
/// P9.2 — rule R3: every roster student needs a grade row first; the 400 lists each missing student as a
/// <c>missingStudentIds</c> detail. Drafts become confirmed; confirmed and published rows are left alone.
/// </summary>
internal sealed class ConfirmGroupGradesHandler(
    IGradesDbContext db, GradingAccess access, IAcademicLifecycle lifecycle, TimeProvider clock)
    : IRequestHandler<ConfirmGroupGradesCommand, ConfirmResultDto>
{
    public async Task<ConfirmResultDto> Handle(ConfirmGroupGradesCommand command, CancellationToken cancellationToken)
    {
        if (!command.Caller.IsAdmin)
            throw new ForbiddenException("Only Student Affairs confirms grades.");

        var graded = await access.LoadGroupAsync(command.GroupId, cancellationToken);
        var roster = await lifecycle.GetStudentsInGroupAsync(graded.Group.Id, cancellationToken);
        var grades = await GroupGrades.LoadTrackedAsync(db, graded, roster, cancellationToken);

        var missing = roster.Where(id => !grades.ContainsKey(id)).ToList();
        if (missing.Count > 0)
            throw new ValidationException(missing.Select(id => new ValidationFailure("missingStudentIds", id.ToString())));

        var now = clock.GetUtcNow();
        var drafts = grades.Values.Where(g => g.Status == GradeStatus.Draft).ToList();
        foreach (var draft in drafts)
            draft.Confirm(command.Caller.UserId, now);

        return new ConfirmResultDto(drafts.Count);
    }
}
