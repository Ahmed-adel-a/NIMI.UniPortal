using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Notifications.Application.Abstractions;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Notifications.Application.Notifications.UnreadCount;

internal sealed class GetUnreadCountHandler(INotificationsDbContext db)
    : IRequestHandler<GetUnreadCountQuery, UnreadCountDto>
{
    public async Task<UnreadCountDto> Handle(GetUnreadCountQuery query, CancellationToken cancellationToken)
    {
        var count = await db.Notifications.AsNoTracking()
            .CountAsync(n => n.UserId == query.UserId && !n.IsRead, cancellationToken);
        return new UnreadCountDto(count);
    }
}
