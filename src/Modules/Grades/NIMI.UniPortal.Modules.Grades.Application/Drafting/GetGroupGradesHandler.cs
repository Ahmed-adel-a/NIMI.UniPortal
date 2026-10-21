using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Grades.Application.Abstractions;
using NIMI.UniPortal.Modules.Grades.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Grades.Application.Drafting;

internal sealed class GetGroupGradesHandler(
    IGradesDbContext db, GradingAccess access, GroupGradesPresenter presenter, IAcademicLifecycle lifecycle)
    : IRequestHandler<GetGroupGradesQuery, GroupGradesDto>
{
    public async Task<GroupGradesDto> Handle(GetGroupGradesQuery query, CancellationToken cancellationToken)
    {
        var graded = await access.LoadGroupAsync(query.GroupId, cancellationToken);
        await access.EnsureTeacherOrAdminAsync(graded, query.Caller, cancellationToken);

        var roster = await lifecycle.GetStudentsInGroupAsync(graded.Group.Id, cancellationToken);
        var grades = await GroupGrades.LoadAsync(db, graded, roster, cancellationToken);
        return await presenter.PresentAsync(graded, roster, grades, cancellationToken);
    }
}
