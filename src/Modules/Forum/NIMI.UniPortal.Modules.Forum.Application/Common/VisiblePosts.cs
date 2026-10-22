using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Forum.Application.Abstractions;
using NIMI.UniPortal.Modules.Forum.Domain;
using NIMI.UniPortal.SharedKernel;

namespace NIMI.UniPortal.Modules.Forum.Application.Common;

/// <summary>Loads a reply (tracked) only when its thread is visible to the caller; otherwise 404.</summary>
internal sealed class VisiblePosts(IForumDbContext db, ForumAccess access)
{
    public async Task<ForumPost> LoadAsync(Guid postId, Viewer viewer, CancellationToken cancellationToken)
    {
        var post = await db.Posts.SingleOrDefaultAsync(p => p.Id == postId, cancellationToken)
            ?? throw new NotFoundException("Post not found.");
        if (post.IsDeleted && !viewer.IsAdmin)
            throw new NotFoundException("Post not found.");

        _ = await access.VisibleThreadAsync(post.ThreadId, viewer, cancellationToken);
        return post;
    }
}
