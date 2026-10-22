using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Events.Application.Abstractions;
using NIMI.UniPortal.Modules.Events.Application.Common;
using NIMI.UniPortal.Modules.Events.Domain;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Events.Application.Participate;

internal sealed class ListEventsHandler(IEventsDbContext db, EventAudience audience, EventPresenter presenter, TimeProvider clock)
    : IRequestHandler<ListEventsQuery, PagedResult<EventDto>>
{
    public async Task<PagedResult<EventDto>> Handle(ListEventsQuery query, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var events = db.Events.AsNoTracking().Where(e => e.Status == EventStatus.Published && e.EndsAt >= now);
        if (!query.Caller.IsAdmin)
        {
            var facultyId = await audience.FacultyOfAsync(query.Caller, cancellationToken);
            events = events.Where(e => e.FacultyId == null || e.FacultyId == facultyId);
        }

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
            await presenter.PresentAllAsync(page, query.Caller.UserId, cancellationToken), query.Paging.Page, query.Paging.PageSize, total);
    }
}
