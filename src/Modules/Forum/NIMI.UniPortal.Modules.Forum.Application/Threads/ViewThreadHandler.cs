using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Forum.Application.Abstractions;
using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Forum.Application.Threads;

internal sealed class ViewThreadHandler(IForumDbContext db, ForumAccess access, TimeProvider clock)
    : IRequestHandler<ViewThreadCommand, ThreadDto>
{
    public async Task<ThreadDto> Handle(ViewThreadCommand command, CancellationToken cancellationToken)
    {
        var thread = await access.VisibleThreadAsync(command.ThreadId, command.Caller, cancellationToken);
        thread.MarkSeenBy(command.Caller.UserId, clock.GetUtcNow());

        var posts = db.Posts.AsNoTracking().Where(p => p.ThreadId == thread.Id);
        var total = await posts.CountAsync(cancellationToken);
        var page = await posts
            .OrderBy(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Skip(command.Paging.Skip)
            .Take(command.Paging.PageSize)
            .ToListAsync(cancellationToken);

        return ThreadDto.From(thread, new PagedResult<PostDto>(
            [.. page.Select(p => PostDto.From(p, command.Caller.IsAdmin))], command.Paging.Page, command.Paging.PageSize, total));
    }
}
