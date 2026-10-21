namespace NIMI.UniPortal.Modules.Notifications.Domain;

/// <summary>
/// One stored message for one recipient (notifications.notifications). Created only by the
/// NotificationRequested handler; read state changes are the only mutation (rule R5).
/// </summary>
public sealed class Notification
{
    private Notification()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>The recipient's Users id — cross-module id, no FK.</summary>
    public Guid UserId { get; private set; }

    public NotificationType Type { get; private set; }

    public string Title { get; private set; } = null!;

    public string TitleAr { get; private set; } = null!;

    public string Message { get; private set; } = null!;

    public string MessageAr { get; private set; } = null!;

    /// <summary>Relative app path (e.g. <c>/assignments/{id}</c>) — never an absolute URL.</summary>
    public string? ActionUrl { get; private set; }

    /// <summary>The <c>NotificationRequested.EventId</c>; with <see cref="UserId"/> it makes delivery idempotent.</summary>
    public Guid SourceEventId { get; private set; }

    public bool IsRead { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Notification Create(
        Guid userId,
        NotificationType type,
        string title,
        string titleAr,
        string message,
        string messageAr,
        string? actionUrl,
        Guid sourceEventId,
        DateTimeOffset now)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("A recipient is required.", nameof(userId));
        if (!Enum.IsDefined(type))
            throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown notification type.");

        return new Notification
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Type = type,
            Title = Required(title, nameof(title), 200),
            TitleAr = Required(titleAr, nameof(titleAr), 200),
            Message = Required(message, nameof(message), 2000),
            MessageAr = Required(messageAr, nameof(messageAr), 2000),
            ActionUrl = NormalizeActionUrl(actionUrl),
            SourceEventId = sourceEventId,
            IsRead = false,
            CreatedAt = now,
        };
    }

    /// <summary>Marks the notification read; the first read time is kept (rule R5 — idempotent).</summary>
    public void MarkRead(DateTimeOffset now)
    {
        if (IsRead)
            return;

        IsRead = true;
        ReadAt = now;
    }

    private static string Required(string value, string paramName, int maxLength)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
            throw new ArgumentException("A value is required.", paramName);
        if (trimmed.Length > maxLength)
            throw new ArgumentException($"At most {maxLength} characters.", paramName);
        return trimmed;
    }

    private static string? NormalizeActionUrl(string? actionUrl)
    {
        if (actionUrl is null)
            return null;

        var trimmed = actionUrl.Trim();
        if (trimmed.Length == 0 || trimmed.Length > 500 || !trimmed.StartsWith('/'))
            throw new ArgumentException("A relative app path starting with '/', at most 500 characters.", nameof(actionUrl));
        return trimmed;
    }
}
