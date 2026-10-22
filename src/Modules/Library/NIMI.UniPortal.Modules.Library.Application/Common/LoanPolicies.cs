using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Library.Application.Abstractions;
using NIMI.UniPortal.Modules.Library.Domain;
using NIMI.UniPortal.Modules.Users.Contracts;

namespace NIMI.UniPortal.Modules.Library.Application.Common;

/// <summary>Members are active students and professors; their loan rules are the saved row or the product default.</summary>
internal sealed class LoanPolicies(ILibraryDbContext db, IUsersApi users, TimeProvider clock)
{
    /// <summary>The member's policy role (<c>student</c>/<c>professor</c>), or null when the user can't borrow.</summary>
    public async Task<string?> RoleOfAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.GetUserAsync(userId, cancellationToken);
        return user is { IsActive: true } && LoanPolicy.Roles.Contains(user.Role) ? user.Role : null;
    }

    public async Task<LoanPolicy> ForRoleAsync(string role, CancellationToken cancellationToken) =>
        await db.Policies.AsNoTracking().SingleOrDefaultAsync(p => p.Role == role, cancellationToken)
        ?? LoanPolicy.Default(role, clock.GetUtcNow());
}
