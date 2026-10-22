using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Forum.Application.Abstractions;
using NIMI.UniPortal.Modules.Forum.Domain;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;

namespace NIMI.UniPortal.Modules.Forum.Application.Common;

/// <summary>
/// Who sees what: members see active university-wide categories and their own faculty's; deleted threads exist only for
/// Student Affairs. Anything invisible is 404. Moderation: Student Affairs, or a teacher of a subject-linked thread's
/// subject in the active term.
/// </summary>
internal sealed class ForumAccess(IForumDbContext db, IUsersApi users, ISchedulingApi scheduling)
{
    public async Task<Guid?> FacultyOfAsync(Viewer viewer, CancellationToken cancellationToken)
    {
        if (viewer.IsAdmin)
            return null;

        var user = await users.GetUserAsync(viewer.UserId, cancellationToken);
        if (user?.DepartmentId is not { } departmentId)
            return null;

        return (await users.GetDepartmentAsync(departmentId, cancellationToken))?.FacultyId;
    }

    public async Task<ForumCategory> VisibleCategoryAsync(Guid categoryId, Viewer viewer, CancellationToken cancellationToken)
    {
        var category = await db.Categories.AsNoTracking().SingleOrDefaultAsync(c => c.Id == categoryId, cancellationToken);
        if (category is null || (!viewer.IsAdmin && !await IsVisibleAsync(category, viewer, cancellationToken)))
            throw new NotFoundException("Category not found.");

        return category;
    }

    public async Task<ForumThread> VisibleThreadAsync(Guid threadId, Viewer viewer, CancellationToken cancellationToken)
    {
        var thread = await db.Threads.SingleOrDefaultAsync(t => t.Id == threadId, cancellationToken);
        if (thread is null || (thread.IsDeleted && !viewer.IsAdmin))
            throw new NotFoundException("Thread not found.");

        _ = await VisibleCategoryAsync(thread.CategoryId, viewer, cancellationToken);
        return thread;
    }

    public async Task EnsureCanModerateAsync(ForumThread thread, Viewer viewer, CancellationToken cancellationToken)
    {
        if (viewer.IsAdmin)
            return;

        if (viewer.IsProfessor && thread.SubjectId is { } subjectId &&
            await scheduling.GetCurrentTermAsync(cancellationToken) is { } term &&
            await scheduling.IsTeachingAsync(viewer.UserId, subjectId, term.Id, null, cancellationToken))
            return;

        throw new ForbiddenException("Only Student Affairs, or the subject's teachers on a subject thread, moderate threads.");
    }

    private async Task<bool> IsVisibleAsync(ForumCategory category, Viewer viewer, CancellationToken cancellationToken) =>
        category.IsActive &&
        (category.FacultyId is null || category.FacultyId == await FacultyOfAsync(viewer, cancellationToken));
}
