using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Notifications.Application.Abstractions;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Notifications.Application.Notifications.ListMine;

internal sealed class ListMyNotificationsHandler(INotificationsDbContext db)
    : IRequestHandler<ListMyNotificationsQuery, PagedResult<NotificationDto>>
{
    public async Task<PagedResult<NotificationDto>> Handle(ListMyNotificationsQuery query, CancellationToken cancellationToken)
    {
        var notifications = db.Notifications.AsNoTracking()
            .Where(n => n.UserId == query.UserId && (!query.UnreadOnly || !n.IsRead));

        var total = await notifications.CountAsync(cancellationToken);
        var items = await notifications
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)                  // Guid v7: deterministic tie-breaker
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .Select(n => new NotificationDto(
                n.Id, n.Type, n.Title, n.TitleAr, n.Message, n.MessageAr, n.ActionUrl, n.IsRead, n.ReadAt, n.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<NotificationDto>(items, query.Paging.Page, query.Paging.PageSize, total);
    }
}
