using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Events.Application.Abstractions;
using NIMI.UniPortal.Modules.Events.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Events.Application.Manage;

internal sealed class ListAdminEventsHandler(IEventsDbContext db, EventPresenter presenter)
    : IRequestHandler<ListAdminEventsQuery, PagedResult<EventDto>>
{
    public async Task<PagedResult<EventDto>> Handle(ListAdminEventsQuery query, CancellationToken cancellationToken)
    {
        var events = db.Events.AsNoTracking();
        if (EventStatuses.Parse(query.Status) is { } status)
            events = events.Where(e => e.Status == status);
        if (query.From is { } from)
            events = events.Where(e => e.StartsAt >= from);
        if (query.To is { } to)
            events = events.Where(e => e.StartsAt <= to);

        var total = await events.CountAsync(cancellationToken);
        var page = await events
            .OrderBy(e => e.StartsAt)
            .ThenBy(e => e.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<EventDto>(
            await presenter.PresentAllAsync(page, query.AdminId, cancellationToken), query.Paging.Page, query.Paging.PageSize, total);
    }
}
