using NIMI.UniPortal.Modules.Forum.Domain;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Forum.Application.Common;

/// <summary>A thread with its body and one page of replies (oldest first).</summary>
public sealed record ThreadDto(
    Guid Id,
    Guid CategoryId,
    Guid? SubjectId,
    string Title,
    string Body,
    AuthorDto Author,
    bool IsPinned,
    bool IsLocked,
    bool IsDeleted,
    int ReplyCount,
    DateTimeOffset LastActivityAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    PagedResult<PostDto> Posts)
{
    public static ThreadDto From(ForumThread thread, PagedResult<PostDto> posts) =>
        new(thread.Id, thread.CategoryId, thread.SubjectId, thread.Title, thread.Body, AuthorDto.From(thread.Author),
            thread.IsPinned, thread.IsLocked, thread.IsDeleted, thread.ReplyCount, thread.LastActivityAt, thread.CreatedAt,
            thread.UpdatedAt, posts);

    public static ThreadDto WithoutPosts(ForumThread thread, PageRequest paging) =>
        From(thread, new PagedResult<PostDto>([], paging.Page, paging.PageSize, 0));
}
