using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Events.Application.Abstractions;
using NIMI.UniPortal.Modules.Events.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Events.Application.Participate;

internal sealed class MyEventsHandler(IEventsDbContext db, EventPresenter presenter) : IRequestHandler<MyEventsQuery, PagedResult<EventDto>>
{
    public async Task<PagedResult<EventDto>> Handle(MyEventsQuery query, CancellationToken cancellationToken)
    {
        var mine = db.Events.AsNoTracking()
            .Where(e => db.Registrations.Any(r => r.EventId == e.Id && r.UserId == query.Caller.UserId));

        var total = await mine.CountAsync(cancellationToken);
        var page = await mine
            .OrderBy(e => e.StartsAt)
            .ThenBy(e => e.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<EventDto>(
            await presenter.PresentAllAsync(page, query.Caller.UserId, cancellationToken), query.Paging.Page, query.Paging.PageSize, total);
    }
}
