using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Assignments.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;

namespace NIMI.UniPortal.Modules.Assignments.Application.Common;

/// <summary>
/// One assignment as seen by one caller (R1, R2): teaching staff and Student Affairs see it; a student sees it when
/// registered in the subject (and, for a group-targeted one, in that group). Anything else — or a deleted assignment —
/// is a 404, so the API never tells a caller that something they may not see exists.
/// </summary>
internal sealed class AssignmentAccess(IAssignmentsDbContext db, CourseAccess course)
{
    public async Task<VisibleAssignment> LoadVisibleAsync(Guid assignmentId, Viewer viewer, CancellationToken cancellationToken)
    {
        var assignment = await db.Assignments.SingleOrDefaultAsync(a => a.Id == assignmentId && !a.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Assignment not found.");

        if (viewer.IsProfessor && await course.TeachesSubjectAsync(viewer.UserId, assignment.SubjectId, assignment.TermId, cancellationToken))
            return new VisibleAssignment(assignment, Teaches: true);

        if (viewer.IsAdmin)
            return new VisibleAssignment(assignment, Teaches: false);

        if (viewer.IsStudent)
        {
            var groups = await course.StudentGroupsAsync(viewer.UserId, assignment.SubjectId, assignment.TermId, cancellationToken);
            if (groups.Count > 0 && (assignment.SubjectGroupId is null || groups.Contains(assignment.SubjectGroupId.Value)))
                return new VisibleAssignment(assignment, Teaches: false);
        }

        throw new NotFoundException("Assignment not found.");
    }

    /// <summary>Changing the assignment or reviewing its work: only its subject's teaching staff (visible but not teaching → 403).</summary>
    public async Task<VisibleAssignment> LoadForTeacherAsync(Guid assignmentId, Viewer viewer, CancellationToken cancellationToken)
    {
        var visible = await LoadVisibleAsync(assignmentId, viewer, cancellationToken);
        return visible.Teaches
            ? visible
            : throw new ForbiddenException("Only the subject's teaching staff can do this.");
    }
}
