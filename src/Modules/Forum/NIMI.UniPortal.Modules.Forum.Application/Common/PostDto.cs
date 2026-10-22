using NIMI.UniPortal.Modules.Forum.Domain;

namespace NIMI.UniPortal.Modules.Forum.Application.Common;

/// <summary>A reply. A removed reply keeps its author and time but shows no body to members (placeholder).</summary>
public sealed record PostDto(
    Guid Id,
    Guid ThreadId,
    AuthorDto Author,
    string? Body,
    bool IsDeleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static PostDto From(ForumPost post, bool showRemoved) =>
        new(post.Id, post.ThreadId, AuthorDto.From(post.Author), post.IsDeleted && !showRemoved ? null : post.Body,
            post.IsDeleted, post.CreatedAt, post.UpdatedAt);
}
