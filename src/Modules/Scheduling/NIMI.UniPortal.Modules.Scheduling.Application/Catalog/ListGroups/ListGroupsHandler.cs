using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.ListGroups;

internal sealed class ListGroupsHandler(ISchedulingDbContext db)
    : IRequestHandler<ListGroupsQuery, PagedResult<GroupDto>>
{
    public async Task<PagedResult<GroupDto>> Handle(ListGroupsQuery query, CancellationToken cancellationToken)
    {
        var subject = await db.Subjects.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == query.SubjectId, cancellationToken)
            ?? throw new NotFoundException($"No subject with id '{query.SubjectId}'.");

        var groups = db.SubjectGroups.AsNoTracking()
            .Where(g => g.SubjectId == query.SubjectId)
            .Where(g => query.TermId == null || g.TermId == query.TermId);

        var total = await groups.CountAsync(cancellationToken);
        var items = await groups
            .OrderBy(g => g.GroupNumber)
            .ThenBy(g => g.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .Select(g => new GroupDto(
                g.Id, g.SubjectId, subject.Code, g.TermId, g.GroupNumber, g.TargetDepartmentId,
                g.Capacity, g.IsActive, 0))
            .ToListAsync(cancellationToken);

        return new PagedResult<GroupDto>(items, query.Paging.Page, query.Paging.PageSize, total);
    }
}
