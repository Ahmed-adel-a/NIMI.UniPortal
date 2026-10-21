using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Facilities.ListFacilities;

internal sealed class ListFacilitiesHandler(ISchedulingDbContext db)
    : IRequestHandler<ListFacilitiesQuery, PagedResult<FacilityDto>>
{
    public async Task<PagedResult<FacilityDto>> Handle(ListFacilitiesQuery query, CancellationToken cancellationToken)
    {
        var facilities = db.Facilities.AsNoTracking()
            .Where(f => query.IncludeInactive || f.IsActive)
            .Where(f => query.Type == null || f.Type == query.Type)
            .Where(f => query.FacultyId == null || f.FacultyId == query.FacultyId || f.FacultyId == null);

        var total = await facilities.CountAsync(cancellationToken);
        var items = await facilities
            .OrderBy(f => f.Name)
            .ThenBy(f => f.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .Select(f => new FacilityDto(
                f.Id, f.FacultyId, f.Name, f.Type, f.Capacity, f.Building, f.Floor, f.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResult<FacilityDto>(items, query.Paging.Page, query.Paging.PageSize, total);
    }
}
