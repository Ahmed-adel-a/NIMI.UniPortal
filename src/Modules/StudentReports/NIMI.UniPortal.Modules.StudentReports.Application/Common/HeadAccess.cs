using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.StudentReports.Application.Common;

/// <summary>The department an active department head supervises, read from Users (not from the token) each time.</summary>
internal sealed class HeadAccess(IUsersApi users)
{
    public async Task<Guid> DepartmentOfAsync(Viewer viewer, CancellationToken cancellationToken)
    {
        if (!viewer.IsDepartmentHead)
            throw new ForbiddenException("Only a department head reads their department's reports.");
        var user = await users.GetUserAsync(viewer.UserId, cancellationToken);
        return user is { IsActive: true, Role: PortalRoles.DepartmentHead, DepartmentId: { } departmentId }
            ? departmentId
            : throw new ForbiddenException("Your account is not the head of a department.");
    }

    /// <summary>The department the viewer heads, or null when they aren't an active head.</summary>
    public async Task<Guid?> DepartmentOrNullAsync(Viewer viewer, CancellationToken cancellationToken)
    {
        if (!viewer.IsDepartmentHead)
            return null;
        var user = await users.GetUserAsync(viewer.UserId, cancellationToken);
        return user is { IsActive: true, Role: PortalRoles.DepartmentHead } ? user.DepartmentId : null;
    }
}
