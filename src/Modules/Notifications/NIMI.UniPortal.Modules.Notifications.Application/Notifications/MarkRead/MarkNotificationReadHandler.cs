using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Notifications.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Notifications.Application.Notifications.MarkRead;

internal sealed class MarkNotificationReadHandler(INotificationsDbContext db, TimeProvider clock)
    : IRequestHandler<MarkNotificationReadCommand, Unit>
{
    public async Task<Unit> Handle(MarkNotificationReadCommand command, CancellationToken cancellationToken)
    {
        // Filtered by the caller's id: an id that exists but belongs to someone else must look unknown.
        var notification = await db.Notifications
            .SingleOrDefaultAsync(n => n.Id == command.NotificationId && n.UserId == command.UserId, cancellationToken)
            ?? throw new NotFoundException("Notification not found.");

        notification.MarkRead(clock.GetUtcNow());
        return Unit.Value;
    }
}
