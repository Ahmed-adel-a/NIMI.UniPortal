using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Notifications.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Notifications.Application.Notifications.MarkAllRead;

internal sealed class MarkAllNotificationsReadHandler(INotificationsDbContext db, TimeProvider clock)
    : IRequestHandler<MarkAllNotificationsReadCommand, Unit>
{
    public async Task<Unit> Handle(MarkAllNotificationsReadCommand command, CancellationToken cancellationToken)
    {
        var unread = await db.Notifications
            .Where(n => n.UserId == command.UserId && !n.IsRead)
            .ToListAsync(cancellationToken);

        var now = clock.GetUtcNow();
        foreach (var notification in unread)
            notification.MarkRead(now);

        return Unit.Value;   // the unit of work saves once
    }
}
