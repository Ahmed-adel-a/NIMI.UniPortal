using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.Modules.Users.Domain.Accounts;
using NIMI.UniPortal.SharedKernel;

namespace NIMI.UniPortal.Modules.Users.Application.Accounts;

/// <summary>Decision ROLES2: a department has at most one active head, so its students' reports reach one person.</summary>
internal static class DepartmentHeads
{
    public static async Task EnsureNoOtherActiveHeadAsync(
        IUsersDbContext db, Guid departmentId, Guid? exceptUserId, CancellationToken cancellationToken)
    {
        var taken = await db.Users.AnyAsync(
            u => u.Role == UserRole.DepartmentHead && u.IsActive && u.DepartmentId == departmentId && u.Id != exceptUserId,
            cancellationToken);
        if (taken)
            throw new ConflictException("The department already has an active head. Deactivate the current head first.");
    }
}
