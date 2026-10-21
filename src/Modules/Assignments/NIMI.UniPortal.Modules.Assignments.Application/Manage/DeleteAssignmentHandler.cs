using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Assignments.Application.Abstractions;
using NIMI.UniPortal.Modules.Assignments.Application.Common;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Assignments.Application.Manage;

internal sealed class DeleteAssignmentHandler(IAssignmentsDbContext db, AssignmentAccess access, TimeProvider clock)
    : IRequestHandler<DeleteAssignmentCommand, Unit>
{
    public async Task<Unit> Handle(DeleteAssignmentCommand command, CancellationToken cancellationToken)
    {
        var (assignment, _) = await access.LoadForTeacherAsync(command.AssignmentId, command.Caller, cancellationToken);

        if (await db.Submissions.AnyAsync(s => s.AssignmentId == assignment.Id, cancellationToken))
            throw new ConflictException("Students have already submitted to this assignment.");

        assignment.Delete(hasSubmissions: false, clock.GetUtcNow());
        return Unit.Value;
    }
}
