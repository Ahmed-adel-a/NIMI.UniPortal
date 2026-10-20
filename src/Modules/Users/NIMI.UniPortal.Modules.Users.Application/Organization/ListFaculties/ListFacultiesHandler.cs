using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Users.Application.Organization.ListFaculties;

internal sealed class ListFacultiesHandler(IUsersDbContext db) : IRequestHandler<ListFacultiesQuery, PagedResult<FacultyDto>>
{
    public async Task<PagedResult<FacultyDto>> Handle(ListFacultiesQuery query, CancellationToken cancellationToken)
    {
        var faculties = db.Faculties.AsNoTracking().Where(f => query.IncludeInactive || f.IsActive);

        var total = await faculties.CountAsync(cancellationToken);
        var items = await faculties
            .OrderBy(f => f.Code)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .Select(f => new FacultyDto(f.Id, f.Code, f.Name, f.NameAr, f.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResult<FacultyDto>(items, query.Paging.Page, query.Paging.PageSize, total);
    }
}
