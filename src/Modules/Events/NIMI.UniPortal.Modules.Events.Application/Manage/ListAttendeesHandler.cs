using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Events.Application.Abstractions;
using NIMI.UniPortal.Modules.Events.Domain;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Events.Application.Manage;

internal sealed class ListAttendeesHandler(IEventsDbContext db, IUsersApi users)
    : IRequestHandler<ListAttendeesQuery, PagedResult<AttendeeDto>>
{
    public async Task<PagedResult<AttendeeDto>> Handle(ListAttendeesQuery query, CancellationToken cancellationToken)
    {
        if (!await db.Events.AsNoTracking().AnyAsync(e => e.Id == query.EventId, cancellationToken))
            throw new NotFoundException("Event not found.");

        var registrations = db.Registrations.AsNoTracking()
            .Where(r => r.EventId == query.EventId && r.Status != EventRegistrationStatus.Cancelled);
        var total = await registrations.CountAsync(cancellationToken);
        var page = await registrations
            .OrderBy(r => r.RegisteredAt)
            .ThenBy(r => r.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .ToListAsync(cancellationToken);

        var rows = new List<AttendeeDto>(page.Count);
        foreach (var r in page)
        {
            var user = await users.GetUserAsync(r.UserId, cancellationToken);
            rows.Add(new AttendeeDto(r.UserId, user?.FullName ?? string.Empty, user?.StudentCode, r.Status, r.RegisteredAt, r.AttendedAt));
        }

        return new PagedResult<AttendeeDto>(rows, query.Paging.Page, query.Paging.PageSize, total);
    }
}
