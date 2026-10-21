using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.Modules.Users.Domain.Accounts;

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

    public Task<int> CountActiveStudentsAsync(
        Guid? facultyId, Guid? departmentId, CancellationToken cancellationToken = default)
    {
        var students = db.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Student && u.IsActive);

        if (departmentId is { } department)
            students = students.Where(u => u.DepartmentId == department);
        else if (facultyId is { } faculty)
            students = students.Where(u => db.Departments.Any(d => d.Id == u.DepartmentId && d.FacultyId == faculty));

        return students.CountAsync(cancellationToken);
    }

    public async Task<StudentPlacementRef?> GetStudentPlacementAsync(
        Guid studentId, CancellationToken cancellationToken = default)
    {
        var placement = await db.Users.AsNoTracking()
            .Where(u => u.Id == studentId && u.Role == UserRole.Student && u.DepartmentId != null)
            .Join(
                db.Departments.AsNoTracking(),
                u => u.DepartmentId,
                d => (Guid?)d.Id,
                (u, d) => new { u.Id, u.DepartmentId, d.FacultyId, u.EnrollmentYear, u.IsActive })
            .SingleOrDefaultAsync(cancellationToken);

        return placement is null
            ? null
            : new StudentPlacementRef(
                placement.Id,
                placement.DepartmentId!.Value,
                placement.FacultyId,
                placement.EnrollmentYear!.Value,
                placement.IsActive);
    }
}
