using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;

namespace NIMI.UniPortal.Modules.Grades.Application.Common;

/// <summary>
/// Rule R2 — who may see and draft a group's grades: the subject's teaching staff for that group (an assignment for
/// the whole subject or exactly this group) draft; Student Affairs sees every group but never drafts (D3).
/// </summary>
internal sealed class GradingAccess(ISchedulingApi scheduling)
{
    public async Task<GradedGroup> LoadGroupAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var group = await scheduling.GetGroupAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");
        var subject = await scheduling.GetSubjectAsync(group.SubjectId, cancellationToken)
            ?? throw new NotFoundException("Subject not found.");
        return new GradedGroup(group, subject);
    }

    public async Task EnsureTeacherAsync(GradedGroup graded, Viewer caller, CancellationToken cancellationToken)
    {
        if (!await TeachesAsync(graded, caller, cancellationToken))
            throw new ForbiddenException("Only the group's teaching staff enter its grades.");
    }

    public async Task EnsureTeacherOrAdminAsync(GradedGroup graded, Viewer caller, CancellationToken cancellationToken)
    {
        if (!caller.IsAdmin && !await TeachesAsync(graded, caller, cancellationToken))
            throw new ForbiddenException("Only the group's teaching staff and Student Affairs see its grades.");
    }

    private async Task<bool> TeachesAsync(GradedGroup graded, Viewer caller, CancellationToken cancellationToken) =>
        caller.IsProfessor &&
        await scheduling.IsTeachingAsync(
            caller.UserId, graded.Subject.Id, graded.Group.TermId, graded.Group.Id, cancellationToken);
}
