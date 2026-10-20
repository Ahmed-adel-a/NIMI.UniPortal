using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application.Auth.GetCurrentUser;

internal sealed class GetCurrentUserHandler(IUsersDbContext db) : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken) =>
        await (
            from u in db.Users.AsNoTracking()
            join d in db.Departments.AsNoTracking() on u.DepartmentId equals d.Id into departments
            from d in departments.DefaultIfEmpty()
            join f in db.Faculties.AsNoTracking() on d.FacultyId equals f.Id into faculties
            from f in faculties.DefaultIfEmpty()
            where u.Id == query.UserId
            select new CurrentUserDto(
                u.Id, u.FullName, u.Email, u.Role, u.StudentCode, u.AcademicTitle,
                u.DepartmentId, d == null ? null : d.Code,
                d == null ? null : d.FacultyId, f == null ? null : f.Code,
                u.EnrollmentYear, u.MustChangePassword, u.LastLoginAt))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("User not found.");
}
