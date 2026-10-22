using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Forum.Application.Abstractions;
using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Forum.Application.Threads;

internal sealed class ListThreadsHandler(IForumDbContext db, ForumAccess access)
    : IRequestHandler<ListThreadsQuery, PagedResult<ThreadSummaryDto>>
{
    public async Task<PagedResult<ThreadSummaryDto>> Handle(ListThreadsQuery query, CancellationToken cancellationToken)
    {
        _ = await access.VisibleCategoryAsync(query.CategoryId, query.Caller, cancellationToken);

        var threads = db.Threads.AsNoTracking().Where(t => t.CategoryId == query.CategoryId);
        if (!query.Caller.IsAdmin)
            threads = threads.Where(t => !t.IsDeleted);

        var total = await threads.CountAsync(cancellationToken);
        var page = await threads
            .OrderByDescending(t => t.IsPinned)
            .ThenByDescending(t => t.LastActivityAt)
            .ThenByDescending(t => t.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ThreadSummaryDto>(
            [.. page.Select(ThreadSummaryDto.From)], query.Paging.Page, query.Paging.PageSize, total);
    }
}
