using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Notifications.Application.Notifications.UnreadCount;

/// <summary>How many of the caller's notifications are still unread (rule R4).</summary>
public sealed record GetUnreadCountQuery(Guid UserId) : IQuery<UnreadCountDto>;
