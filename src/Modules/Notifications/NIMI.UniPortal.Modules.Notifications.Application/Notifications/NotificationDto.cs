using NIMI.UniPortal.Modules.Notifications.Domain;

namespace NIMI.UniPortal.Modules.Notifications.Application.Notifications;

/// <summary>One notification as the signed-in user sees it. <c>type</c> serializes as snake_case text.</summary>
public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string TitleAr,
    string Message,
    string MessageAr,
    string? ActionUrl,
    bool IsRead,
    DateTimeOffset? ReadAt,
    DateTimeOffset CreatedAt);
