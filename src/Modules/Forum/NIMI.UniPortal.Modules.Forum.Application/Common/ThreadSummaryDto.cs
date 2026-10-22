using NIMI.UniPortal.Modules.Forum.Domain;

namespace NIMI.UniPortal.Modules.Forum.Application.Common;

/// <summary>A thread in a category list (no body).</summary>
public sealed record ThreadSummaryDto(
    Guid Id,
    Guid CategoryId,
    Guid? SubjectId,
    string Title,
    AuthorDto Author,
    bool IsPinned,
    bool IsLocked,
    bool IsDeleted,
    int ReplyCount,
    DateTimeOffset LastActivityAt,
    DateTimeOffset CreatedAt)
{
    public static ThreadSummaryDto From(ForumThread thread) =>
        new(thread.Id, thread.CategoryId, thread.SubjectId, thread.Title, AuthorDto.From(thread.Author), thread.IsPinned,
            thread.IsLocked, thread.IsDeleted, thread.ReplyCount, thread.LastActivityAt, thread.CreatedAt);
}
