using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Notifications.Application.Abstractions;
using NIMI.UniPortal.Modules.Notifications.Contracts;
using NIMI.UniPortal.Modules.Notifications.Domain;

namespace NIMI.UniPortal.Modules.Notifications.Application.IntegrationEvents;

/// <summary>
/// P2.1 — stores one notification per distinct recipient, skipping recipients that already have a row for
/// the same <c>EventId</c> (rule R3; the unique index backs it up). Saves once, as event handlers do.
/// Invalid events (unknown type, empty/oversized texts) are logged and dropped — never thrown.
/// </summary>
internal sealed class NotificationRequestedHandler(
    INotificationsDbContext db,
    TimeProvider clock,
    ILogger<NotificationRequestedHandler> logger) : IIntegrationEventHandler<NotificationRequested>
{
    public async Task HandleAsync(NotificationRequested integrationEvent, CancellationToken cancellationToken)
    {
        if (!TryParseType(integrationEvent.Type, out var type))
        {
            logger.LogWarning(
                "Dropping notification event {EventId}: unknown type '{Type}'.",
                integrationEvent.EventId,
                integrationEvent.Type);
            return;
        }

        var recipients = integrationEvent.RecipientUserIds.Distinct().ToArray();
        if (recipients.Length == 0)
            return;

        var alreadyStored = await db.Notifications.AsNoTracking()
            .Where(n => n.SourceEventId == integrationEvent.EventId && recipients.Contains(n.UserId))
            .Select(n => n.UserId)
            .ToListAsync(cancellationToken);

        List<Notification> notifications;
        try
        {
            var now = clock.GetUtcNow();
            notifications = recipients
                .Except(alreadyStored)
                .Select(userId => Notification.Create(
                    userId,
                    type,
                    integrationEvent.Title,
                    integrationEvent.TitleAr,
                    integrationEvent.Message,
                    integrationEvent.MessageAr,
                    integrationEvent.ActionUrl,
                    integrationEvent.EventId,
                    now))
                .ToList();
        }
        catch (ArgumentException exception)
        {
            // Nothing was added yet: build every row before touching the change tracker, so an invalid
            // event stores nothing at all.
            logger.LogWarning(exception, "Dropping invalid notification event {EventId}.", integrationEvent.EventId);
            return;
        }

        if (notifications.Count == 0)
            return;

        db.Notifications.AddRange(notifications);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static bool TryParseType(string value, out NotificationType type)
    {
        type = default;
        return !int.TryParse(value, out _)
            && Enum.TryParse(value, ignoreCase: true, out type)
            && Enum.IsDefined(type);
    }
}
