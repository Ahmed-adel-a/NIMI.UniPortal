using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Forum.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;

namespace NIMI.UniPortal.Modules.Forum.Application.Common;

/// <summary>At most 10 new threads and posts per user in any 60 seconds; the next one is 429.</summary>
internal static class PostingRateLimit
{
    public const int PerMinute = 10;

    public static async Task EnsureAllowedAsync(IForumDbContext db, Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var since = now.AddMinutes(-1);
        var threads = await db.Threads.AsNoTracking().CountAsync(t => t.Author.Id == userId && t.CreatedAt > since, cancellationToken);
        var posts = await db.Posts.AsNoTracking().CountAsync(p => p.Author.Id == userId && p.CreatedAt > since, cancellationToken);
        if (threads + posts >= PerMinute)
            throw new TooManyRequestsException($"At most {PerMinute} posts a minute. Please wait a moment.");
    }
}
