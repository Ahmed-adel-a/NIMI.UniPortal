namespace NIMI.UniPortal.Modules.Announcements.Domain;

/// <summary>
/// A message from a subject's teaching staff to its students — every group, or one group (decision ANN: professors
/// only, subject-scoped). Expired announcements stay for teachers but disappear for students (R6); deleting is soft.
/// </summary>
public sealed class Announcement
{
    private const int MaxTitleLength = 255;
    private const int MaxBodyLength = 10000;

    private Announcement()
    {
    }

    public Guid Id { get; private set; }

    public Guid SubjectId { get; private set; }

    public Guid TermId { get; private set; }

    /// <summary>Null = every group of the subject.</summary>
    public Guid? SubjectGroupId { get; private set; }

    public Guid AuthorId { get; private set; }

    public string Title { get; private set; } = null!;

    public string Body { get; private set; } = null!;

    public AnnouncementPriority Priority { get; private set; }

    public bool IsPinned { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Announcement Create(
        Guid subjectId,
        Guid termId,
        Guid? subjectGroupId,
        Guid authorId,
        string title,
        string body,
        AnnouncementPriority priority,
        bool isPinned,
        DateTimeOffset? expiresAt,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            SubjectId = subjectId,
            TermId = termId,
            SubjectGroupId = subjectGroupId,
            AuthorId = authorId,
            Title = Text(title, MaxTitleLength, "title"),
            Body = Text(body, MaxBodyLength, "body"),
            Priority = Defined(priority),
            IsPinned = isPinned,
            ExpiresAt = Expiry(expiresAt, now),
            CreatedAt = now,
        };

    public void Edit(string title, string body, AnnouncementPriority priority, bool isPinned, DateTimeOffset? expiresAt, DateTimeOffset now)
    {
        var newTitle = Text(title, MaxTitleLength, "title");
        var newBody = Text(body, MaxBodyLength, "body");
        var newPriority = Defined(priority);
        var newExpiry = Expiry(expiresAt, now);

        Title = newTitle;
        Body = newBody;
        Priority = newPriority;
        IsPinned = isPinned;
        ExpiresAt = newExpiry;
        UpdatedAt = now;
    }

    /// <summary>Soft delete; deleting twice changes nothing.</summary>
    public void Delete(DateTimeOffset now)
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        UpdatedAt = now;
    }

    public bool IsExpiredAt(DateTimeOffset now) => ExpiresAt is { } expiry && expiry <= now;

    private static string Text(string value, int maxLength, string field)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
            throw new ArgumentException("Required.", field);
        return trimmed.Length <= maxLength
            ? trimmed
            : throw new ArgumentException($"At most {maxLength} characters.", field);
    }

    private static AnnouncementPriority Defined(AnnouncementPriority priority) =>
        Enum.IsDefined(priority) ? priority : throw new ArgumentOutOfRangeException("priority", priority, "Unknown priority.");

    private static DateTimeOffset? Expiry(DateTimeOffset? expiresAt, DateTimeOffset now) =>
        expiresAt is null || expiresAt > now
            ? expiresAt
            : throw new ArgumentException("The expiry must be in the future.", "expiresAt");
}
