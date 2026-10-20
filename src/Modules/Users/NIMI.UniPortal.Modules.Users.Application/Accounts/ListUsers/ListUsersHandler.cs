using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Users.Application.Accounts.ListUsers;

internal sealed class ListUsersHandler(IUsersDbContext db) : IRequestHandler<ListUsersQuery, PagedResult<UserDto>>
{
    public async Task<PagedResult<UserDto>> Handle(ListUsersQuery query, CancellationToken cancellationToken)
    {
        var users =
            from u in db.Users.AsNoTracking()
            join d in db.Departments.AsNoTracking() on u.DepartmentId equals d.Id into departments
            from d in departments.DefaultIfEmpty()
            join f in db.Faculties.AsNoTracking() on d.FacultyId equals f.Id into faculties
            from f in faculties.DefaultIfEmpty()
            select new { User = u, Department = d, Faculty = f };

        if (query.Role is { } role)
            users = users.Where(x => x.User.Role == role);
        if (query.DepartmentId is { } departmentId)
            users = users.Where(x => x.User.DepartmentId == departmentId);
        if (query.FacultyId is { } facultyId)
            users = users.Where(x => x.Department != null && x.Department.FacultyId == facultyId);
        if (query.IsActive is { } isActive)
            users = users.Where(x => x.User.IsActive == isActive);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            users = users.Where(x =>
                x.User.FullName.ToLower().Contains(term)
                || x.User.Email.Contains(term)
                || (x.User.StudentCode != null && x.User.StudentCode.ToLower().Contains(term)));
        }

        var total = await users.CountAsync(cancellationToken);
        var items = await users
            .OrderByDescending(x => x.User.CreatedAt).ThenBy(x => x.User.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .Select(x => new UserDto(
                x.User.Id, x.User.FullName, x.User.Email, x.User.Role, x.User.StudentCode, x.User.AcademicTitle,
                x.User.DepartmentId,
                x.Department == null ? null : x.Department.Code,
                x.Faculty == null ? null : x.Faculty.Code,
                x.User.EnrollmentYear, x.User.IsActive, x.User.MustChangePassword, x.User.CreatedAt, x.User.LastLoginAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<UserDto>(items, query.Paging.Page, query.Paging.PageSize, total);
    }
}
