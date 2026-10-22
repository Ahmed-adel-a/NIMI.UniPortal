using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Abstractions;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Standings;

internal sealed class ListStandingsHandler(ISemesterDbContext db, IUsersApi users)
    : IRequestHandler<ListStandingsQuery, PagedResult<StandingDto>>
{
    public async Task<PagedResult<StandingDto>> Handle(ListStandingsQuery query, CancellationToken cancellationToken)
    {
        var standings = db.Standings.AsNoTracking();
        if (query.DepartmentId is { } departmentId)
            standings = standings.Where(s => s.DepartmentId == departmentId);
        if (query.Level is { } level)
            standings = standings.Where(s => s.Level == level);
        if (query.Status is { } status)
            standings = standings.Where(s => s.Status == status);

        var total = await standings.CountAsync(cancellationToken);
        var page = await standings
            .OrderBy(s => s.DepartmentId).ThenBy(s => s.Level).ThenBy(s => s.StudentId)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .ToListAsync(cancellationToken);

        var items = new List<StandingDto>(page.Count);
        foreach (var standing in page)
            items.Add(await StandingReader.ToDtoAsync(standing, users, cancellationToken));

        return new PagedResult<StandingDto>(items, query.Paging.Page, query.Paging.PageSize, total);
    }
}
