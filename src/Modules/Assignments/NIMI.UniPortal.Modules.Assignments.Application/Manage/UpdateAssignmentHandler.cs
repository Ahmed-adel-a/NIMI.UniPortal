using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Assignments.Application.Abstractions;
using NIMI.UniPortal.Modules.Assignments.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Assignments.Application.Manage;

internal sealed class UpdateAssignmentHandler(
    IAssignmentsDbContext db, AssignmentAccess access, AssignmentPresenter presenter, TimeProvider clock)
    : IRequestHandler<UpdateAssignmentCommand, AssignmentDto>
{
    public async Task<AssignmentDto> Handle(UpdateAssignmentCommand command, CancellationToken cancellationToken)
    {
        var (assignment, _) = await access.LoadForTeacherAsync(command.AssignmentId, command.Caller, cancellationToken);
        var hasSubmissions = await db.Submissions.AnyAsync(s => s.AssignmentId == assignment.Id, cancellationToken);

        assignment.Update(
            command.Title,
            command.Instructions,
            command.DueAt,
            command.MaxGrade,
            command.AllowedExtensions,
            command.MaxFileMb,
            hasSubmissions,
            clock.GetUtcNow());

        return await presenter.PresentAsync(assignment, command.Caller, staffView: true, cancellationToken);
    }
}
