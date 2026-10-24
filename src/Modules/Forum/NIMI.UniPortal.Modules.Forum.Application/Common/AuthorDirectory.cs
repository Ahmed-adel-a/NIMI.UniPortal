using NIMI.UniPortal.Modules.Forum.Domain;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Forum.Application.Common;

/// <summary>
/// Decision D4 — builds the identity every thread and post shows: the user's name, rank (<c>student</c>, the academic
/// title of teaching staff, <c>student_affairs</c>) and department, read from Users at writing time.
/// </summary>
internal sealed class AuthorDirectory(IUsersApi users)
{
    public async Task<ForumAuthor> ForAsync(Viewer viewer, CancellationToken cancellationToken)
    {
        var user = await users.GetUserAsync(viewer.UserId, cancellationToken);
        if (viewer.IsAdmin)
            return new ForumAuthor(viewer.UserId, user?.FullName ?? "Student Affairs", "student_affairs", "Student Affairs", "شؤون الطلاب");

        if (user is null)
            throw new ForbiddenException("Your account was not found.");

        var department = user.DepartmentId is { } id ? await users.GetDepartmentAsync(id, cancellationToken) : null;
        var rank = !viewer.IsProfessor ? "student"
            : user.Role == PortalRoles.DepartmentHead ? PortalRoles.DepartmentHead   // decision ROLES2
            : user.AcademicTitle ?? "professor";
        return new ForumAuthor(user.Id, user.FullName, rank, department?.Name ?? string.Empty, department?.NameAr ?? string.Empty);
    }
}
