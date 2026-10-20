using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Users.Application.Organization.ListDepartments;

internal sealed class ListDepartmentsHandler(IUsersDbContext db) : IRequestHandler<ListDepartmentsQuery, PagedResult<DepartmentDto>>
{
    public async Task<PagedResult<DepartmentDto>> Handle(ListDepartmentsQuery query, CancellationToken cancellationToken)
    {
        var departments =
            from d in db.Departments.AsNoTracking()
            join f in db.Faculties.AsNoTracking() on d.FacultyId equals f.Id
            where (query.FacultyId == null || d.FacultyId == query.FacultyId)
                  && (query.IncludeInactive || d.IsActive)
            select new { Department = d, FacultyCode = f.Code };

        var total = await departments.CountAsync(cancellationToken);
        var items = await departments
            .OrderBy(x => x.Department.Code)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .Select(x => new DepartmentDto(
                x.Department.Id, x.Department.FacultyId, x.FacultyCode, x.Department.Code,
                x.Department.Name, x.Department.NameAr, x.Department.Description, x.Department.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResult<DepartmentDto>(items, query.Paging.Page, query.Paging.PageSize, total);
    }
}
