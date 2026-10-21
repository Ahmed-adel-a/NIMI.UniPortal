using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Notifications.Application.Notifications.ListMine;

/// <summary>The caller's own notifications, newest first; optionally only unread ones (rule R4).</summary>
public sealed record ListMyNotificationsQuery(Guid UserId, bool UnreadOnly, PageRequest Paging)
    : IQuery<PagedResult<NotificationDto>>;
