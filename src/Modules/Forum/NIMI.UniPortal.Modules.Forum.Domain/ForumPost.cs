namespace NIMI.UniPortal.Modules.Forum.Domain;

/// <summary>A reply in a thread. Same 15-minute author edit window as threads; deleting is soft.</summary>
public sealed class ForumPost
{
    private ForumPost()
    {
    }

    public Guid Id { get; private set; }

    public Guid ThreadId { get; private set; }

    public ForumAuthor Author { get; private set; } = null!;

    public string Body { get; private set; } = string.Empty;

    public bool IsDeleted { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static ForumPost Write(Guid threadId, ForumAuthor author, string body, DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            ThreadId = threadId,
            Author = author,
            Body = ForumText.Required(body, ForumText.MaxBodyLength, nameof(body)),
            CreatedAt = now,
        };

    public bool CanAuthorEdit(Guid userId, DateTimeOffset now) =>
        Author.Id == userId && now - CreatedAt <= TimeSpan.FromMinutes(ForumText.EditWindowMinutes);

    public void Edit(string body, DateTimeOffset now)
    {
        Body = ForumText.Required(body, ForumText.MaxBodyLength, nameof(body));
        UpdatedAt = now;
    }

    public void Delete(DateTimeOffset now)
    {
        IsDeleted = true;
        UpdatedAt = now;
    }
}
