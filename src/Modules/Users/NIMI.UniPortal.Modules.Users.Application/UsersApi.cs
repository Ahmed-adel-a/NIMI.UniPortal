using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.Modules.Users.Contracts;

namespace NIMI.UniPortal.Modules.Users.Application;

/// <summary>The Users module's public contract for other modules: read-only, DTOs only, never IQueryable (trap #6).</summary>
internal sealed class UsersApi(IUsersDbContext db) : IUsersApi
{
    public Task<FacultyRef?> GetFacultyAsync(Guid facultyId, CancellationToken cancellationToken = default) =>
        db.Faculties.AsNoTracking()
            .Where(f => f.Id == facultyId)
            .Select(f => new FacultyRef(f.Id, f.Code, f.Name, f.NameAr, f.IsActive))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<DepartmentRef?> GetDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default) =>
        db.Departments.AsNoTracking()
            .Where(d => d.Id == departmentId)
            .Select(d => new DepartmentRef(d.Id, d.FacultyId, d.Code, d.Name, d.NameAr, d.IsActive))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<UserRef?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        return user is null
            ? null
            : new UserRef(
                user.Id, user.FullName, user.Role.ToString().ToLowerInvariant(), user.DepartmentId, user.StudentCode,
                user.AcademicTitle?.ToString().ToLowerInvariant(), user.IsActive);
    }
}
