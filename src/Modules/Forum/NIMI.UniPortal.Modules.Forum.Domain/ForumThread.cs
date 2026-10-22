namespace NIMI.UniPortal.Modules.Forum.Domain;

/// <summary>
/// A discussion in a category, optionally about one subject. Its author may edit or delete it for 15 minutes; moderators
/// pin and lock it; a locked or deleted thread takes no replies. Deleting is soft (moderation keeps the history).
/// </summary>
public sealed class ForumThread
{
    private ForumThread()
    {
    }

    public Guid Id { get; private set; }

    public Guid CategoryId { get; private set; }

    /// <summary>Scheduling subject id (cross-schema id, no FK) for subject discussions; its teachers may moderate.</summary>
    public Guid? SubjectId { get; private set; }

    public ForumAuthor Author { get; private set; } = null!;

    public string Title { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public bool IsPinned { get; private set; }

    public bool IsLocked { get; private set; }

    public bool IsDeleted { get; private set; }

    public int ReplyCount { get; private set; }

    public DateTimeOffset LastActivityAt { get; private set; }

    /// <summary>When the author was last told about a reply; with <see cref="AuthorSeenAt"/> it keeps one unread notice per thread.</summary>
    public DateTimeOffset? AuthorNotifiedAt { get; private set; }

    /// <summary>When the author last opened the thread.</summary>
    public DateTimeOffset? AuthorSeenAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static ForumThread Start(Guid categoryId, Guid? subjectId, ForumAuthor author, string title, string body, DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            CategoryId = categoryId,
            SubjectId = subjectId,
            Author = author,
            Title = ForumText.Required(title, ForumText.MaxTitleLength, nameof(title)),
            Body = ForumText.Required(body, ForumText.MaxBodyLength, nameof(body)),
            LastActivityAt = now,
            CreatedAt = now,
        };

    public bool CanAuthorEdit(Guid userId, DateTimeOffset now) =>
        Author.Id == userId && now - CreatedAt <= TimeSpan.FromMinutes(ForumText.EditWindowMinutes);

    public void Edit(string title, string body, DateTimeOffset now)
    {
        Title = ForumText.Required(title, ForumText.MaxTitleLength, nameof(title));
        Body = ForumText.Required(body, ForumText.MaxBodyLength, nameof(body));
        UpdatedAt = now;
    }

    public void Delete(DateTimeOffset now)
    {
        IsDeleted = true;
        UpdatedAt = now;
    }

    public void SetPinned(bool pinned, DateTimeOffset now)
    {
        IsPinned = pinned;
        UpdatedAt = now;
    }

    public void SetLocked(bool locked, DateTimeOffset now)
    {
        IsLocked = locked;
        UpdatedAt = now;
    }

    /// <summary>
    /// Counts a reply and says whether to notify the author: never for their own reply, and only when they have opened
    /// the thread since the last notice (at most one unread notice per thread).
    /// </summary>
    public bool AcceptReply(ForumAuthor replier, DateTimeOffset now)
    {
        if (IsDeleted)
            throw new InvalidOperationException("This thread was removed.");
        if (IsLocked)
            throw new InvalidOperationException("This thread is locked.");

        ReplyCount++;
        LastActivityAt = now;

        var notify = replier.Id != Author.Id &&
                     (AuthorNotifiedAt is null || (AuthorSeenAt is { } seen && seen >= AuthorNotifiedAt));
        if (notify)
            AuthorNotifiedAt = now;
        return notify;
    }

    public void MarkSeenBy(Guid userId, DateTimeOffset now)
    {
        if (userId == Author.Id)
            AuthorSeenAt = now;
    }
}
